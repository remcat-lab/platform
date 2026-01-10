Custom DataGrid처럼 반복되는 텍스트가 많고 스크롤 성능이 중요한 컨트롤에서 Text = WriteableBitmap(WB) 캐시, 기타 그래픽 = DrawingGroup 조합은 성능과 유연성을 모두 잡을 수 있는 최적의 선택입니다.
특히 120x40 크기의 텍스트 셀은 비트맵 캐싱의 효율이 극대화되는 지점입니다. 결론을 아래 표로 정리해 드립니다.
렌더링 전략 비교 및 결론
구분	텍스트 (Text Content)	일반 그래픽 (라인, 배경, 테두리)
권장 방식	WriteableBitmap (WB) 캐시	DrawingGroup (Direct Drawing)
저장 형태	Dictionary<Key, BitmapSource> (LRU)	즉시 실행 명령 또는 DrawingGroup
선택 이유	텍스트 래스터화는 CPU 부하가 큼. RDP 환경에서 폰트 연산 생략이 필수적임.	선(Line)이나 사각형은 연산이 매우 가볍고, 셀 크기에 따라 동적으로 변해야 함.
RDP 이점	동일 단어 재사용 시 전송량 급감, 서버 CPU 절약.	데이터량이 적어 벡터 명령 전송에 유리.
메모리 효율	120x40 기준 약 19KB로 매우 효율적.	거의 차지하지 않음.
확대/축소	품질 저하 가능성 (DPI 대응 필요).	벡터 방식이라 무한히 선명함.
최종 구현을 위한 핵심 가이드라인
텍스트 (WB 캐시):
Key 생성: (글자 내용 + 폰트 정보 + 컬러 + DPI)를 조합하여 Unique Key를 생성하세요.
생성 절차: FormattedText → DrawingVisual → RenderTargetBitmap → WriteableBitmap → Freeze() 순서로 진행합니다.
그리기: drawingContext.DrawImage(cachedWB, rect)를 호출합니다.
배경 및 테두리 (DrawingGroup/Direct):
drawingContext.DrawRectangle이나 DrawLine을 사용하세요. 이 작업은 픽셀 데이터를 만드는 것보다 좌표를 보내는 것이 훨씬 빠릅니다.
선택 영역(Selection)이나 호버(Hover) 효과처럼 실시간으로 변하는 그래픽은 캐싱하지 않고 즉시 그리는 것이 메모리에 이롭습니다.
DPI 대응 (중요):
RDP 환경이나 고해상도 모니터에서는 DPI가 1.0(96 DPI)이 아닐 수 있습니다. 비트맵 생성 시 VisualTreeHelper.GetDpi(this)를 반영하지 않으면 텍스트가 흐릿해 보입니다.
예상 결과
이 하이브리드 방식을 적용하면, DataGrid 스크롤 시 CPU 점유율이 기존 대비 50% 이상 낮아지며, 특히 사양이 낮은 클라이언트나 RDP 환경에서 끊김 없는(Smooth) 스크롤을 구현할 수 있습니다.
