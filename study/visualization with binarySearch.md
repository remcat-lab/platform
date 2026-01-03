데이터그리드 가상화에서 가장 중요한 것은 "수만 개의 행 중 현재 화면(Viewport)에 보이는 10~20개의 행이 몇 번째 인덱스인가?"를 빛의 속도로 찾아내는 것입니다.
질문하신 정렬된 인덱스(sortIndices)와 누적 높이(Accumulated Offset)를 이용한 가상화 로직을 C# 예시 코드와 함께 상세히 설명합니다.
1. 가상화를 위한 데이터 구조 준비
먼저, 정렬과 필터링이 완료된 후의 상태를 관리하는 구조가 필요합니다.
csharp
public class VirtualizationManager
{
    private int[] _sortIndices;          // 정렬/필터링된 결과 인덱스 배열
    private double[] _accumulatedOffsets; // 각 행의 시작 Y 좌표 (누적합)
    private double _totalHeight;          // 전체 데이터의 총 높이

    // 1. 정렬 및 오프셋 계산 (데이터가 바뀌거나 정렬될 때 1회 실행)
    public void RefreshLayout(int[] sortedIndices, Func<int, double> getRowHeight)
    {
        _sortIndices = sortedIndices;
        _accumulatedOffsets = new double[sortedIndices.Length];
        
        double currentY = 0;
        for (int i = 0; i < sortedIndices.Length; i++)
        {
            _accumulatedOffsets[i] = currentY;
            // 각 행의 높이를 더해 다음 행의 시작 위치를 계산
            currentY += getRowHeight(sortedIndices[i]); 
        }
        _totalHeight = currentY;
    }
}
코드를 사용할 때는 주의가 필요합니다.

2. BinarySearch를 이용한 가시 범위 계산 (핵심)
사용자가 스크롤을 움직여 ViewportTop(스크롤 위치)이 변경되면, _accumulatedOffsets 배열에서 이진 탐색(Binary Search)을 통해 화면에 보일 첫 번째 행을 찾습니다.
csharp
public (int startIndex, int endIndex) GetVisibleRange(double viewportTop, double viewportHeight)
{
    if (_accumulatedOffsets == null || _accumulatedOffsets.Length == 0) 
        return (0, 0);

    // 1. 이진 탐색으로 현재 스크롤 위치(Y)에 걸쳐 있는 첫 번째 행의 인덱스를 찾음
    // _accumulatedOffsets는 오름차순으로 정렬되어 있으므로 BinarySearch 가능 (O(log N))
    int index = Array.BinarySearch(_accumulatedOffsets, viewportTop);

    // 정확한 값이 없으면 비트 보수(~index)가 삽입 지점을 가리킴
    int startIndex = index < 0 ? Math.Max(0, (~index) - 1) : index;

    // 2. 마지막으로 보일 행의 인덱스 찾기
    double viewportBottom = viewportTop + viewportHeight;
    int endIndex = startIndex;
    
    for (int i = startIndex; i < _accumulatedOffsets.Length; i++)
    {
        endIndex = i;
        // 현재 행의 시작 위치가 뷰포트 하단을 벗어나면 중단
        if (_accumulatedOffsets[i] > viewportBottom) break;
    }

    return (startIndex, endIndex);
}
코드를 사용할 때는 주의가 필요합니다.

3. 화면 렌더링 시 활용
UI 스레드에서는 계산된 startIndex부터 endIndex까지만 루프를 돌며 Items 원본에서 데이터를 가져와 그립니다.
csharp
public void Render(DrawingContext dc, double viewportTop)
{
    var (start, end) = GetVisibleRange(viewportTop, 300); // 300은 화면 높이

    for (int i = start; i <= end; i++)
    {
        // 원본 데이터에서 실제 인덱스 추출
        int actualDataIndex = _sortIndices[i];
        var item = _sourceItems[actualDataIndex];

        // 해당 행의 Y 위치
        double yPos = _accumulatedOffsets[i];
        
        // 실제 그리기 로직 (예: 가상 좌표를 기준으로 Draw)
        // DrawRow(dc, item, yPos - viewportTop);
    }
}
코드를 사용할 때는 주의가 필요합니다.

4. 이 방식이 왜 빠른가?
Binary Search (O(log N)): 행이 1,000,000개일 때, 순차 탐색은 최악의 경우 100만 번 비교해야 하지만, 이진 탐색은 단 20번의 비교로 시작점을 찾습니다. C# Array.BinarySearch는 이 알고리즘을 고도로 최적화하여 제공합니다.
메모리 효율: _accumulatedOffsets는 double[] 타입의 연속된 메모리 블록이므로 CPU 캐시 적중률이 매우 높습니다.
가변 높이 대응: 모든 행의 높이가 달라도(Variable Row Height) 오프셋 배열이 미리 계산되어 있으므로, 검색 성능에 영향이 전혀 없습니다.
요약하자면
이 메커니즘은 "미리 계산된 좌표 배열"을 지도 삼아, "이진 탐색"이라는 빠른 수단으로 현재 화면에 필요한 데이터만 쏙쏙 뽑아내는 방식입니다. 이를 통해 100만 건 이상의 데이터에서도 프레임 드랍 없는 부드러운 스크롤을 구현할 수 있습니다. 고성능 UI 가상화 기술 가이드에서 관련 개념을 더 상세히 확인할 수 있습니다.
