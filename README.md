- Split the project into separate Domain/Application/Infrastructure projects if the codebase grows —
  this would enforce dependency direction at compile time.
- Handle 401 responses from the Catalog API by automatically re-authenticating and retrying the
  request, instead of only checking whether a token had been previously obtained.
- Replace in-memory storage with a database (or Redis) so baskets survive an app restart and the
  API can run as more than one instance. If using Entity Framework, configure Guid generation via
  Fluent API rather than relying on manual Guid.NewGuid() calls in application code.
- Use a lock (Monitor) around basket mutations to protect against race conditions from concurrent
  requests on the same basket.