Improvements:

- Split the project into separate Domain/Application/Infrastructure projects if the codebase grows.
- Handle 401 responses from the Catalog API by automatically re-authenticating and retrying the request, instead of only checking whether a token had been previously obtained.
- Use a lock around basket mutations to protect against race conditions from concurrent requests on the same basket.
- Move item-mutation logic (add/update/remove, TotalPrice recalculation) out of BasketController and into BasketService, so controllers only orchestrate calls and don't hold business logic.