# CarshiTow Security Production Readiness (PROUD)

This document is the production-readiness guide for the Auth/Security stack.
It explains:

- What is already implemented
- What is still missing for enterprise-grade production
- How to add/configure each missing piece (DB, Redis, deployment, etc.)

---

## 1) Current Security Status (Implemented)

The project currently includes:

- JWT access tokens
- Refresh token rotation
- Refresh token storage in secure cookies
- CSRF validation for refresh/logout flow
- Password hashing via BCrypt
- OTP + MFA flow
- Device fingerprinting and trusted-device flow
- Global exception handling with problem details
- Security headers middleware
- Input validation with FluentValidation
- Input sanitization (HtmlSanitizer)
- Rate limiting policies
- Brute-force mitigation (now distributed-cache ready via Redis/IDistributedCache)

This is a strong baseline, but not yet full enterprise production.

---

## 2) Missing / Needed for Full Enterprise Production

### A. Database Hardening & Operations

Missing / weak areas:

- No migration pipeline strategy documented/enforced
- No DB backup/restore policy
- SQLite is acceptable for dev, but production typically needs PostgreSQL or SQL Server
- No connection resiliency / retry strategy policy

How to add:

1. Choose production DB:
   - Recommended: PostgreSQL or SQL Server
2. Add provider package:
   - PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL`
   - SQL Server: `Microsoft.EntityFrameworkCore.SqlServer`
3. Update `AddDbContext` with provider + retry:
   - `EnableRetryOnFailure(...)`
4. Add migration workflow:
   - `dotnet ef migrations add <Name>`
   - `dotnet ef database update`
5. In CI/CD, run migrations before traffic shift.
6. Define backup policy:
   - RPO/RTO targets
   - daily full + transaction log/incremental backups
   - quarterly restore drill

---

### B. Redis (Brute-force + Future Security State)

Missing / weak areas:

- Redis enabled toggle exists, but no health-check gating
- No HA/cluster guidance
- No TLS/auth requirements documented

How to add:

1. Configure production `appsettings.Production.json`:
   - `Redis:Enabled=true`
   - `Redis:Configuration=<host:port,password=...,ssl=True,...>`
2. Use managed Redis (Azure Cache / AWS ElastiCache / Redis Enterprise)
3. Enforce:
   - TLS in transit
   - AUTH/password/ACL
   - private networking only (no public endpoint)
4. Add health checks:
   - fail readiness when Redis unavailable
5. Add key policy:
   - strict TTL
   - prefix isolation by environment (`prod:carshitow:*`)

---

### C. Secrets & Key Management

Missing / weak areas:

- JWT/Twilio keys may still be config-file based
- No key rotation runbook documented

How to add:

1. Never keep secrets in repo/appsettings.
2. Use secret manager:
   - Azure Key Vault / AWS Secrets Manager / HashiCorp Vault
3. Inject secrets through environment or provider.
4. Rotate:
   - JWT signing key
   - Twilio credentials
   - DB/Redis credentials
5. Implement dual-key JWT validation window during rotation.

---

### D. Authentication & Session Hardening

Missing / weak areas:

- No explicit refresh token reuse detection alerting path
- No account-level persistent lockout table in DB
- No step-up auth policy for high-risk actions

How to add:

1. Add persistent security tables:
   - `SecurityEvents`
   - `AccountLockouts`
   - `SuspiciousSessions`
2. Record refresh-token reuse attempts and revoke session family.
3. Add risk-based controls:
   - impossible travel/device anomaly
   - force MFA on high risk
4. Add step-up MFA for sensitive operations (password/email/phone changes).

---

### E. API Edge Protection

Missing / weak areas:

- No WAF/CDN policy in front of API
- No bot-management controls
- No strict CORS policy documented

How to add:

1. Put API behind gateway/WAF:
   - Azure Front Door + WAF / AWS CloudFront + WAF / Cloudflare
2. Enable managed rules:
   - SQLi/XSS/common exploit signatures
3. Configure strict CORS:
   - allow only known front-end origins
   - no wildcard in prod
4. Add request size limits and content-type enforcement.

---

### F. Observability, Alerting, and Audit

Missing / weak areas:

- No defined SIEM/audit stream
- No security alert thresholds/runbook

How to add:

1. Log security events as structured logs:
   - login_fail
   - otp_fail
   - lockout_triggered
   - refresh_reuse_detected
2. Ship logs to SIEM:
   - Sentinel / Splunk / Datadog / ELK
3. Define alerts:
   - spikes in 401/429
   - lockout bursts by IP/subnet
   - Twilio OTP send anomalies
4. Keep immutable audit trail for compliance.

---

### G. Deployment & Environment Model

Missing / weak areas:

- No formal environment promotion policy
- No zero-downtime deployment process documented
- No rollback runbook

How to add:

1. Environments:
   - `dev` -> `staging` -> `prod`
2. CI pipeline:
   - restore/build/test/security scan/SAST
3. CD pipeline:
   - deploy to staging
   - smoke + integration tests
   - manual approval
   - blue/green or canary to prod
4. Rollback:
   - one-click previous stable revision
   - schema rollback strategy if migration incompatible

---

### H. Container/Kubernetes Readiness (if using K8s)

Missing / weak areas:

- No readiness/liveness/startup probes documented
- No pod security and resource policy documented

How to add:

1. Add health endpoints:
   - `/health/live`
   - `/health/ready` (checks DB + Redis)
2. Set probes in deployment spec.
3. Set CPU/memory requests/limits.
4. Use non-root container user.
5. Add NetworkPolicy to isolate API/DB/Redis paths.

---

### I. Compliance / Governance

Missing / weak areas:

- No explicit retention and PII data policy
- No threat model artifact

How to add:

1. Create threat model (STRIDE) for auth flow.
2. Classify PII and define retention/deletion policy.
3. Add secure SDLC controls:
   - dependency scanning
   - secret scanning
   - periodic penetration test
4. Define incident response runbook (on-call + escalation).

---

## 3) Minimal Production Checklist (Go-Live Gate)

Do not go live until all are done:

- [ ] Production DB provider selected and migration pipeline validated
- [ ] Redis enabled with TLS/auth/private network
- [ ] Secrets moved to secret manager
- [ ] Strict CORS and WAF configured
- [ ] Health checks + readiness gates implemented
- [ ] CI/CD with staged rollout and rollback in place
- [ ] Security alerting + audit logs integrated
- [ ] Backup/restore drill completed
- [ ] Pen-test/high-risk auth tests completed

---

## 4) Suggested Next Implementation Order

1. Production DB migration (PostgreSQL/SQL Server)
2. Redis health checks + readiness gating
3. Secret manager integration
4. WAF + strict CORS + gateway controls
5. Persistent security events + lockout tables
6. CI/CD hardened release pipeline
7. SIEM alerts + incident runbooks

---

## 5) Notes

- SQL injection risk is low in current EF Core usage (parameterized LINQ), but WAF + secure coding + tests are still required.
- Brute-force controls are now much better with distributed cache support, but enterprise scale requires Redis HA and monitoring.
- Treat this file as a living production-readiness checklist.
