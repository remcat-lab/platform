Visual DataGrid에서 대용량 데이터를 다룰 때 원본 컬렉션을 변경하지 않고 인덱스 배열(sortIndices)만 정렬하는 방식은 메모리 복사 비용을 줄이고 정렬 속도를 극대화할 수 있는 매우 효율적인 방법입니다.
특히 MemoryMarshal.Cast와 ArrayPool<byte>를 활용하면 힙(Heap) 할당을 최소화하고 CPU 캐시 효율을 높일 수 있습니다.
1. 핵심 개념 및 원리
원본 보존: Items 원본 리스트는 순서를 유지합니다. 대신 int[] indices를 만들어 [0, 1, 2, ...]로 초기화한 뒤 이 인덱스들을 정렬합니다.
MemoryMarshal.Cast: byte[] 메모리를 double이나 long(DateTime용) 배열인 것처럼 속여서(재해석하여) 포인터 연산으로 접근합니다. 이 과정에서 새로운 배열 할당이 일어나지 않습니다.
Span 기반 Sort: MemoryExtensions.Sort는 Span<T>를 지원하며, 두 개의 Span(키 Span과 값 Span)을 함께 정렬하는 기능을 제공합니다. 이를 통해 정렬 기준값(Key)을 정렬할 때 인덱스(Value)도 같이 움직이게 합니다.
2. 구현 예제 (C#)
아래 코드는 double 타입의 컬럼 데이터를 기준으로 인덱스를 정렬하는 예시입니다.
csharp
using System;
using System.Buffers;
using System.Runtime.InteropServices;

public class DataGridIndexer
{
    public void SortByDoubleColumn(ReadOnlySpan<double> columnData, Span<int> indices)
    {
        // 1. 인덱스 초기화 (0, 1, 2, 3...)
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        // 2. 임시 키 배열 준비 (ArrayPool 사용으로 GC 부하 감소)
        // columnData를 직접 정렬하면 안 되므로 복사본 필요
        double[] tempKeys = ArrayPool<double>.Shared.Rent(columnData.Length);
        try
        {
            Span<double> keySpan = tempKeys.AsSpan(0, columnData.Length);
            columnData.CopyTo(keySpan);

            // 3. 핵심: keySpan을 정렬할 때 indices를 함께 정렬
            // MemoryExtensions.Sort(Keys, Values) 사용
            keySpan.Sort(indices);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(tempKeys);
        }
    }

    // DateTime 처리를 위한 MemoryMarshal 활용 예시
    public void SortByDateTimeColumn(ReadOnlySpan<DateTime> dateData, Span<int> indices)
    {
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        // DateTime은 내부적으로 long(8byte)과 크기가 같음
        int count = dateData.Length;
        long[] tempKeys = ArrayPool<long>.Shared.Rent(count);
        
        try
        {
            // DateTime Span을 long Span으로 캐스팅 (복사 없이 타입만 재해석)
            ReadOnlySpan<long> longData = MemoryMarshal.Cast<DateTime, long>(dateData);
            
            Span<long> keySpan = tempKeys.AsSpan(0, count);
            longData.CopyTo(keySpan);

            // 정렬
            keySpan.Sort(indices);
        }
        finally
        {
            ArrayPool<long>.Shared.Return(tempKeys);
        }
    }
}
코드를 사용할 때는 주의가 필요합니다.

3. 상세 설명
ArrayPool vs ArrayPool:
질문에서 언급하신 ArrayPool<byte>를 가져와 MemoryMarshal.Cast<byte, double>을 하는 방식은 보통 네트워크 패킷이나 파일 I/O에서 가져온 raw 데이터를 다룰 때 씁니다. 일반적인 DataGrid 상황에서는 ArrayPool<double>.Shared.Rent(count)를 사용하는 것이 더 직관적이고 안전합니다.
MemoryMarshal.Cast의 역할:
DateTime 컬럼을 정렬할 때, DateTime 객체 자체를 비교하는 것보다 내부 Ticks값인 long으로 캐스팅하여 정렬하는 것이 CPU 연산에서 미세하게 더 빠를 수 있습니다. MemoryMarshal.Cast<DateTime, long>(dateData)는 메모리 복사 없이 데이터의 해석 방식만 바꿉니다.
성능 이점:
GC 압력 감소: ArrayPool을 사용하여 대규모 정렬 시 발생하는 임시 배열 할당을 없앱니다.
참조 로컬리티: 원본 객체(Item) 전체를 이동시키는 것이 아니라, 연속된 메모리 공간에 있는 double 값과 int 인덱스만 교환하므로 CPU 캐시 히트율이 압도적으로 높습니다.
UI 반응성: 정렬된 indices만 가지고 DataGrid의 GetItemAt(indices[row]) 형태로 뷰를 갱신하면 대량의 데이터에서도 즉각적인 정렬이 가능합니다.
4. 주의사항 (2026년 기준 권장사항)
Data Consistency: 원본 Items 리스트에 아이템이 추가/삭제되면 indices 배열도 갱신(Re-build)해야 합니다.
Stable Sort: MemoryExtensions.Sort는 기본적으로 불안정 정렬(Unstable Sort)입니다. 동일한 값이 있을 때 기존 순서 유지가 중요하다면 별도의 처리가 필요합니다.
이 방식은 Microsoft의 고성능 가이드라인에서 권장하는 Span<T> 활용 패턴의 핵심입니다. 정렬 후 필터링을 할 때는 indices를 순회하며 조건에 맞지 않는 인덱스를 제외한 새로운 filterIndices를 생성하면 됩니다.
