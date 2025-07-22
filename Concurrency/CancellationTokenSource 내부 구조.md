# CancellationTokenSource 내부 구조와 동작

## 📚 핵심 개념
`CancellationTokenSource`는
✅ 취소 신호를 생성하고,
✅ 이를 여러 작업(Task, Thread, async 메서드 등)에 전달할 수 있도록
`CancellationToken`을 만들어 주는 역할을 한다.

---

## 🔍 내부 구성 요소

### 1️⃣ 상태 관리
- 취소 상태는 내부적으로 `int` 변수(`m_state`)로 관리
  - `0` : 아직 취소되지 않음
  - `1` : 취소 신호 발생됨
  - `2` : 이미 disposed 상태
- 상태 변경은 원자적 연산(Interlocked)으로 수행

---

### 2️⃣ 콜백 목록
- `CancellationToken.Register()`를 통해 콜백 등록 가능
- 콜백들은 `CancellationTokenSource` 내부에 저장됨
  - 자료구조: `SparselyPopulatedArray<CancellationCallbackInfo>`
- `Cancel()` 호출 시 등록된 콜백을 순서대로 실행

---

### 3️⃣ WaitHandle (Optional)
- `WaitHandle` (`ManualResetEventSlim`)을 가짐
- 다른 스레드가 취소를 기다릴 수 있도록 함
  - 예: `token.WaitHandle.WaitOne()`
- `Cancel()` 호출 시 시그널
- `ManualResetEventSlim`이 커널 리소스를 사용할 수 있기 때문에 `Dispose()` 필요

---

### 4️⃣ Timer (Optional)
- `CancelAfter()` 호출 시 내부적으로 `Timer` 설정
- 일정 시간이 지나면 자동으로 `Cancel()` 호출
- `Dispose()` 시 `Timer`도 해제

---

### 5️⃣ Linked Tokens
- `CreateLinkedTokenSource()`로 여러 토큰을 연결 가능
- 내부적으로 연결된 토큰의 콜백에 `this.Cancel()`을 등록

---

## ⚙️ 주요 메서드 동작

### `Cancel()`
- 상태를 `0 → 1`로 설정 (Interlocked)
- 콜백 배열을 복사 후 순서대로 실행
- `WaitHandle` 시그널
- `Timer`가 설정돼 있으면 해제

---

### `Dispose()`
- `WaitHandle`과 `Timer`를 정리
- 상태를 `2`로 설정

---

## 📝 요약 다이어그램

CancellationTokenSource
├── 상태 플래그 (int)
├── 콜백 목록 (SparselyPopulatedArray)
├── WaitHandle (ManualResetEventSlim)
├── Timer (System.Threading.Timer)
└── Linked Tokens

Cancel()
├── 상태 변경 → 콜백 호출 → WaitHandle 시그널

Dispose()
├── WaitHandle 및 Timer 해제

---

## 🔗 왜 이렇게 설계되었나?
- `CancellationToken`은 `struct`라서 경량, 값 복사로 빠르게 전달
- 콜백을 통해 async/await 및 Task에서 취소 감지 가능
- 동기/비동기 양쪽 모두에서 사용 가능
- 리소스를 명시적으로 해제하도록 `IDisposable` 구현

