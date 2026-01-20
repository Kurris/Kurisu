### New Rules

| Rule ID | Category | Severity | Notes |
|---------|----------|----------|-------|
| KS1001  | Correctness | Error | Calling a method with `Propagation.Mandatory` requires an ambient method annotated with `[Transactional]` on the call chain. |
