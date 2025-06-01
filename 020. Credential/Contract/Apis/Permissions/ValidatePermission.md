
# API 권한 제어 정책 설계 문서 (비트 기반 확장 최종안)

## 1. 개요

이 문서는 API Gateway 및 백엔드 서비스의 **접근 제어 정책**을 정의합니다.  
부서(Department)와 사용자(User)를 기준으로 한 정책 기반 권한 관리 구조이며, 다음을 목표로 합니다:

- ✅ 보안 우선 (Deny 우선 원칙)
- ✅ 유연한 개인 권한 예외 처리
- ✅ 비트 연산 기반 확장성
- ✅ 고성능 URL Prefix 매칭

---

## 2. 권한 우선순위 모델

| 부서 상태(Status)   | 설명 |
|---------------------|------|
| **Deny (0b001)**     | 무조건 차단 (개인 권한 무시) |
| **Allow (0b010)**    | 개인 권한이 거부일 경우 우선 거부, 그 외는 허용 |
| **DefaultDeny (0b100)** | 개인 권한이 승인일 경우만 허용, 그 외 거부 |

> ⚠️ `Status`는 비트 플래그로 저장하며, AND/OR 연산으로 비교합니다.  
예: `Allow | DefaultDeny = 0b110` → 조건부 허용 의미 가능

---

## 3. 권한 평가 흐름 (비트 연산 기반 의사코드)

```pseudo
const DENY = 0b001
const ALLOW = 0b010
const DEFAULT_DENY = 0b100

function hasAccess(userId, departmentId, url):
    deptStatus = getDeptStatusPrefixMatch(departmentId, url)
    userStatus = getUserStatusPrefixMatch(userId, url)

    if (deptStatus & DENY) == DENY:
        return false

    if (deptStatus & ALLOW) == ALLOW:
        if (userStatus & DENY) == DENY:
            return false
        return true

    if (deptStatus & DEFAULT_DENY) == DEFAULT_DENY:
        if (userStatus & ALLOW) == ALLOW:
            return true
        return false
```

---

## 4. 테이블 구조

```text
ACL_Department
--------------
DepartmentId | UrlPrefix   | Status  | ExpireDate

ACL_User
--------
UserId       | UrlPrefix   | Status  | ExpireDate
```

- **UrlPrefix**: `/api/v1/user/` 처럼 특정 경로로 시작하는 URL을 의미 (Prefix 방식으로 고정)
- **Status**: 3비트 이진값 (예: 0b010 = Allow)
- **ExpireDate**: 권한 만료일 (해당 일자 이후 무효)

### Status 비트 정의

| 상태명          | 비트값 | 의미                                       |
|----------------|--------|--------------------------------------------|
| Deny           | 0b001  | 명시적 거부, 우선 적용                     |
| Allow          | 0b010  | 승인, 단 개인 거부가 있을 경우 무효화됨    |
| DefaultDeny    | 0b100  | 명시적 승인 없이는 거부                    |

> 예: `Status = 0b110 (Allow | DefaultDeny)` → 조건부 허용 정책

---

## 5. 평가 예시

| 부서 상태 | 사용자 상태 | 최종 결과 | 설명 |
|-----------|--------------|-----------|------|
| 0b001 (Deny)   | 0b010 (Allow)   | ❌ 거부     | 부서가 Deny면 무조건 차단 |
| 0b010 (Allow)  | 없음            | ✅ 허용     | 부서가 허용, 사용자 예외 없음 |
| 0b010 (Allow)  | 0b001 (Deny)    | ❌ 거부     | 사용자 거부 우선 |
| 0b100 (DefaultDeny) | 0b010 (Allow) | ✅ 허용     | 사용자 명시적 허용 |
| 0b100 (DefaultDeny) | 없음         | ❌ 거부     | 사용자 승인 없으므로 차단 |

---

## 6. 고급 기능 (선택 적용)

### ✅ 메서드 제어

- `UrlPrefix` + `Method (GET, POST...)` 조합도 지원 가능
- Status를 Method별로 세분화하고, 테이블에 필드 추가 가능

### ✅ 역할 기반 (RBAC) 확장

- `ACL_Role`, `UserRoles`, `DepartmentRoles` 테이블로 역할 기반 권한 위임 가능

### ✅ 캐시 적용

- 부서/사용자 권한 정보를 Redis 또는 메모리 캐시로 관리
- 정책 변경 시 TTL 또는 수동 무효화

---

## 7. 장점 요약

| 항목                     | 설명 |
|--------------------------|------|
| ✅ **보안 우선**             | Deny 비트 우선 처리 |
| ✅ **성능 최적화**           | Prefix 매칭 + 비트 연산 |
| ✅ **확장성 우수**           | Method, Role, Cache 등 다양한 기능과 연동 가능 |
| ✅ **직관적 구조**           | 테이블 2개로 충분히 표현 |
| ✅ **RBAC 연동 가능**        | 역할 기반 관리에도 손쉬운 확장 가능 |

---

## 8. 결론

비트 기반 `Status` 필드와 URL Prefix 매칭은 권한 판단 로직을 **효율적이고 확장 가능하게** 만들어줍니다.  
추가로 Method별 분기 및 역할 기반 확장도 가능한 구조로, **고성능, 고보안, 고유연성**을 갖춘 API 접근 제어가 가능합니다.
