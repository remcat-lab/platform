2026년 현재 최신 .NET 개발 환경에서 MemoryExtensions.Sort와 List<T>.Sort의 차이점 및 인덱스 기반 정렬 방식에 대해 설명해 드립니다.
1. MemoryExtensions.Sort vs. List.Sort 비교
구분	List<T>.Sort	MemoryExtensions.Sort (Span/Memory)
대상	List<T> 객체 전용	Span<T>, ReadOnlySpan<T> (메모리 슬라이스)
할당(Allocation)	리스트 구조에 종속적	힙 할당 없이 메모리 범위 내에서 직접 수행
유연성	리스트 전체 혹은 특정 범위만 가능	배열, 스택 메모리, 비관리 메모리 등 모든 연속된 데이터 정렬 가능
성능	일반적인 상황에서 표준적 성능	불필요한 복사나 참조가 적어 대용량 데이터에서 더 효율적임
List<T>.Sort: 내부 배열을 정렬하며, 객체 지향적인 방식으로 리스트 데이터 자체를 관리할 때 사용합니다. Microsoft의 List.Sort 공식 문서에서 상세 구현을 확인할 수 있습니다.
MemoryExtensions.Sort: Span<T>에 대한 확장 메서드로 제공됩니다. 배열의 일부분이나 스택에 할당된 메모리(stackalloc)도 별도의 복사본 생성 없이 즉시 정렬할 수 있어 고성능 처리에 유리합니다. Span 정렬에 대한 기술 가이드를 참고하세요.
2. 값의 변화 없이 Index만 변화시키는 정렬 (간접 정렬)
질문하신 "값은 그대로 두고 인덱스만 변화시킨다"는 개념은 데이터가 크거나 구조체(Struct)일 때, 데이터를 직접 교체(Swap)하는 비용을 줄이기 위해 사용하는 인덱스 정렬(Indirect Sort) 기법입니다.
작동 원리
원본 데이터: [ "Banana", "Apple", "Cherry" ] (변경하고 싶지 않음)
인덱스 배열 생성: 원본의 방 번호인 [ 0, 1, 2 ]를 만듭니다.
비교 정렬: 인덱스 배열을 정렬하되, 비교 기준은 원본 데이터의 값으로 합니다.
0번(Banana)과 1번(Apple)을 비교 -> Apple이 우선이므로 인덱스 순서를 [1, 0, 2]로 바꿉니다.
결과: 원본 배열은 그대로지만, 인덱스 배열 [1, 0, 2]를 순서대로 읽으면 Apple, Banana, Cherry 순으로 정렬된 결과를 얻을 수 있습니다.
구현 예시 (C#)
csharp
int[] values = { 40, 10, 30, 20 };
int[] indices = { 0, 1, 2, 3 };

// 인덱스 배열을 정렬하되, 비교는 values의 값을 사용
Array.Sort(indices, (a, b) => values[a].CompareTo(values[b]));

// 결과 확인: values는 그대로 { 40, 10, 30, 20 }
// indices는 { 1, 3, 2, 0 } (값이 작은 순서대로의 인덱스)
코드를 사용할 때는 주의가 필요합니다.

이 방식은 원본 데이터의 불변성을 유지해야 하거나, 데이터 하나의 크기가 너무 커서 정렬 시 발생하는 데이터 이동(Copy) 비용이 인덱스 정렬 비용보다 클 때 매우 유용합니다. 보다 정밀한 메모리 제어가 필요하다면 C# Memory와 Span 활용법을 확인해 보시기 바랍니다.


정말 빠르게 하고 싶다면 이렇게 해야 함
✅ Decorate–Sort–Undecorate (DSU)
csharp

접기
저장
복사
1
2
3
4
5
6
7
var keys = new long[n];
for (int i = 0; i < n; i++)
{
    keys[i] = dateTimes[i].Ticks;
}

Array.Sort(keys, indices);
❗ 성능 차이:

기존: comparator 기반 n log n
DSU: primitive compares → 2~5배 빠름
