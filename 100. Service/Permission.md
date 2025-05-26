
# API 권한 제어 정책 설계 문서

## 1. 개요

이 문서는 API Gateway 및 서비스의 권한 제어를 위해 제안된 정책 구조를 설명합니다. 본 설계는 부서(Department)와 사용자(User)를 기준으로 API 접근 권한을 평가하며, 유연성과 보안성을 모두 고려한 방식입니다.

---

## 2. 권한 우선순위 모델

| 부서 권한       | 처리 방식 |
|----------------|------------|
| **거부(Deny)**   | 무조건 접근 거부 (개인 권한 무시) |
| **승인(Allow)** | 개인 권한 중 **거부가 있으면 우선 거부**, 없으면 허용 |
| **보류(Pending)** | 개인 권한 중 **승인만 처리**, 없으면 거부 |

### 권한 평가 흐름 (의사코드)

```pseudo
function hasAccess(userId, departmentId, url):
    deptPerm = getDeptPermission(departmentId, url)
    userPerm = getUserPermission(userId, url)

    if deptPerm == "거부":
        return false
    elif deptPerm == "승인":
        if userPerm == "거부":
            return false
        return true
    elif deptPerm == "보류":
        if userPerm == "승인":
            return true
        return false  # 개인 권한이 없거나 거부일 경우
```

---

## 3. 테이블 설계 예시

```text
ACL_Department
--------------
DepartmentId | Url         | Permission  -- (거부, 승인, 보류)

ACL_User
---------
UserId       | Url         | Permission  -- (거부, 승인)
```

---

## 4. 평가 예시

| Dept 권한 | User 권한 | 최종 결과 | 설명 |
|-----------|------------|-----------|------|
| 거부       | 승인        | 거부        | 부서가 거부했기 때문에 무조건 차단 |
| 승인       | 없음        | 승인        | 부서가 허용했고, 사용자 예외 없음 |
| 승인       | 거부        | 거부        | 사용자 오버라이드 |
| 보류       | 승인        | 승인        | 사용자 예외 허용 |
| 보류       | 없음        | 거부        | 보류 상태에서 사용자도 승인 안 했으므로 거부 |

---

## 5. 장점 요약

- **정책 주도적 접근**: 부서 중심으로 정책 수립 가능
- **보안 우선순위 준수**: Deny 우선 원칙 유지
- **관리와 제어의 균형**: 실무에서 실용적으로 적용 가능
- **확장성 확보**: 단순한 구조로 다양한 상황에 대응 가능

---

## 6. 결론

이 설계는 Windows 권한 모델과 유사한 정책 기반 접근 방식을 따르며, 리눅스 ACL 확장 모델보다 더 명시적이고 유연한 구조입니다.
