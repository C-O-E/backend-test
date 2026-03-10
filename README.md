# Backend Developer Technical Assessment

**Time guideline**: This assessment is designed for approximately **2-3 hours of work**. Focus on quality over quantity.

## Submission

1. Commit all your changes (code fixes, feature implementations, and written answers in this file)
2. Push your branch
3. Open a **Pull Request** to `main` with a brief summary of your work

## Context

You are joining a team that maintains a compliance and asset management platform. This API is a simple module that manages **asset entities** (legal entities, natural persons), their **relationships** (ownership, directorship, subsidiaries), and **asset ownerships**.

The previous developer left the project in a broken state... Ohlala... Your job is to get it working, fix what you find, and most importanly *demonstrate* how you think. We would like a step-by-step walkthrough of your thought process as you explore the code, find issues, and implement features. Don't worry about perfection, we just want to see how you approach problems and make decisions.

## Getting Started

1. **Prerequisites**: .NET 8 SDK (or can be easily updated to 9 or 10 if you prefer) and any code editor (Visual Studio, VSCode, Rider, etc.)
2. **Clone** this repository and create a new branch named `test/{your-name}` (e.g., `test/john-smith`)
3. **Build**: `dotnet build`
4. **Run**: `dotnet run --project BackTest`
5. **Explore**: Swagger UI is available at the URL shown in the console output (`/swagger`)

(For 3 and 4 you can also use Visual Studio Debug Mode)

/!\ The application uses an **in-memory database** seeded with sample data on startup. Keep that in mind if your data tend to disappear after you restart the app.

## Your Assignment

Work through the parts below in order! For each part, document your findings and reasoning directly in this file under the corresponding section (or in code comments). When you're done, commit your work and **open a Pull Request** back to `main`, there we'll review your changes.

We value your **thought process and reasoning** more than having a perfectly working app. If you find an issue but don't have time to fix it, describe it and explain how you would fix it.

---

### Part 1: Exploration & Bug Fixing

Run the application and explore it using Swagger or any HTTP client (Postman, curl, etc.).

**Tasks:**
- List every issues you find (bugs, design problems, missing features, warnings)
- Fix the ones you can within the time you have
- For each issue, briefly explain: what's wrong, why it matters, and how you fixed it (or would fix it)

**Write your findings here:**
-  To spot the issues, I did the quick discovery of the repository:
   - Check the Controller, Service, Data Models and Persistence (Repositories folder)
   - Then, I start the server, review /swagger link to list provided APIs, there are 2 groups of API: manage AssetEntity and manage Relationship
   - Finally, I tried playing by browser (GET) and curl (other APIs) per API with some brush tests to have overview of APIs, and note down any abnormal behavior
- I found several issues:
   - About repository itself: 
     - There are **no tests**, definitely because it is not production one. Without tests, we are risked of breaking system in new development => I try to put tests on the bug/feature I develop
   - Features missing: 
     - The models are prepared but APIs are `not yet developed`:
        - DELETE /api/AssetEntity/relationship/{relationshipId} (mentioned in feature question and I implemented in this PR)
        - Manage Asset and AssetOwnership
        - Manage EntityPosition, this API group will be under `/api/AssetEntity` like relationship
     - No Authentication and Authorization put in place, the TenantId helping segregate the user request is still easily manipulated by user
   - Bugs and design warnings:  
    1. The GET `/api/AssetEntity` is using TenantId from request header to return entities belonged to this tenant, however when it is null, the response returns entities of **every tenant**, which is security risk
        - Root cause: due to the `|| _currentTenantId == null` in Global query filter in `AssetManagementDbContext.cs` 
        ```csharp
          modelBuilder.Entity<AssetEntity>()
              .HasQueryFilter(e => !e.IsDeleted && (e.Tenant_Id == _currentTenantId || _currentTenantId == null));
        ```
      - Fix: just remove the condition `_currentTenantId == null`
    - The POST `/api/AssetEntity` breaks in any transaction:
      - **Root cause**: The `collection modified` error is caused by `AuditLogs` being updated inside 
      ```csharp
      foreach (var entry in ChangeTracker.Entries<CommonBase>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
      ```
      While `DbSet<AuditLog> AuditLogs` is also managed by `ChangeTracker.Entries`, the complain comes
      - **Fix**: snapshot the loop by `.toList()`, so the change of AuditLogs is immutable to this loop
      ```csharp
      foreach (var entry in ChangeTracker.Entries<CommonBase>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList())
      ```
    2. The POST `/api/AssetEntity` is accepting directly `AssetEntity` in the request. This one is base class for 2 sub type `Legal Entity` and `Natural Entity`. Thanks to the `[JsonDerivedType]` instructions in `Models.cs`, the JSON polymorphic deserialization can build the relevant sub-type object and the saving to the single DB table (TBH) will work. This approach is nice, however there are flaws:
      - **Problems**:
        - The polymorphic deserialization works only when the discriminator `$type` is provided. This keyword is not shown automatically in Swagger, so it is `invisible to user` to know to include it in request. When no `$type` is given, the JSON is deserialized to base class `AssetEntity`, then all legal or natural specific properties are `lost and not saved` to DB.
        - Also regarding Swagger, as `AssetEntity` is shown, client sees only the base, not other sub-type specific fields as `legalName` (of `Legal Entity`), or `FirstName`, `LastName` (of `Natural Entity`)
        - Moreover, the API exposes `too much` properties for user, e.g. `entity_Id` (I experienced conflict error when existing id is saved to DB), or other `commonBase` fields are injectable, while they should be auto-generated by system. I tested and encountered conflict error when choosing `entity_id` that exists => The cleaner approach is to create dedicated DTO to expose only fields client needs
        - We should also avoid coupling between this http request model and the `AssetEntity` internal model for future evolution. Again the dedicated DTO can help here.
      - **Fix** : Based on above analys, I implemented the fix
        - Create DTO `CreateAssetEntityRequest` as base class, and 2 derived classes `CreateLegalEntityRequest` and `CreateNaturalEntityRequest`, that contain fields user can inject
        - Classes `CreateLegalEntityRequest` and `CreateNaturalEntityRequest` are also using [JsonDerivedType] to deserialize polymorphically, the discriminator is declared explicitly `type`. I suppose we will not support if type is missing or not known, so the deserialization will throw in case
        - The `CreateAssetEntityRequest` base class define `abstract AssetEntity ToDomainModel()` so that each sub type can implement specific mapping to the corresponding `AssetType` sub type (i.e `CreateNaturalEntityRequest` -> `LegalEntity`)
        - An improvement to enforce the Tenant_Id at creation time in `AssetEntityController.cs`, and raise `400 Bad Request` if not present
        - Update Swagger generation in `Program.cs` to show `type` keyword mandatory and request is oneOf of 2 schema `CreateNaturalEntityRequest` and `CreateLegalEntityRequest`
        - Create tests in `CreateAssetEntityTests.cs`: normally we should cover by both unittests in each layer, due to time constraint, I chose only component test (integration test): 2 OK cases for creating Legal Entity and Natural Entity + 2 KO cases (missing `type` and missing `Tenant-id`)
      - Future improvement: If time permits, I would create a DTO for the CreateAssetEntityResponse as well, to decouple the response from internal model `AssetEntity`, and hide some fields maybe not necessary for user e.g. relationships, ownedAssets, entityPosition, as they might be retrieved by seperate APIs. Both Request and Response classes might be later generated via OpenAPI libraries.
    3. The PUT `/api/AssetEntity/{id}` is updating only fields of base class `AssetEntity`, the sub class specific fields e.g. `LegalName` (of Legal Entity), `FirstName`, `LastName` (of Natural Entity) will never be updated.
      - **Root causes**:
        - The update code in `AssetEntityService` considers input as `AssetEntity` object, so it could update only `AssetEntity` fields (e.g. `EntityReference`). It is also dangerous that if some fields in request are ommitted, their deserialized value is null, and the current value in DB will be updated to null as consequent. That **violates** the logic of updating 
      - **Fix**: I implemented in this PR
        - Create a DTO `UpdateAssetEntityRequest.cs` that flat out updateable fields of `AssetEntity` and its sub classes. All fields are optional so that they can be omitted in the request
        - To apply the update on which field, the logic is based on the type of updated object `/api/AssetEntity/{id}`, as reasonably we will not change the type by PUT => Method `ApplyTo(AssetEntity existing)` take cares of the update common and subtype specific fields **only if** the provided field is not null (intended change)
        - Integration tests are given in `UpdateAssetEntityTests.cs` : 2 OK cases showing updating `Legal Entity` and `Natural Entity` without caring of other non-related type fields (e.g. `FirstName` in `Legal Entity`) + 1 KO case showing updating locked entity without success
    4. The `PreventModificationOfLockedEntities` in `AssetManagementDbContext.cs` is **dead-code**, I suppose the expected logic is to prevent the updating (so PUT API(s)) in locked entity => I integrated this method at the beginning of `SaveChangeAsync` method to stop modification before any further action.
    ```
    Note: In my opinion, locked entity is being frozen for some reasons, we should not delete locked one. Thus, I expand state Deleted to the PreventModificationOfLockedEntities as well
    ```
    5. GET `/api/AssetEntity/{id}/relationships` and `/api/AssetEntity/{id}/relationships/indirect` returns full `SourceEntity` and `TargetEntity` objects, which is too verbose and unncessarily resource consuming, because only SourceEntity_Id and TargetEntity_Id are sufficient for direct/indirect relationship algorithm
    - **Root cause** : Inside method `GetIndirectRelationshipsAsync`, there is population
    ```csharp
    .Include(r => r.SourceEntity)
    .Include(r => r.TargetEntity)
    ```
    This population is waste of resource for unncessary JOIN
    - **Fix** : As this is read only query, I just removed .Include() and used AsNoTracking to return lean result
    ```csharp
    var relationships = await _context.Relationships
                .AsNoTracking()
                .Where(r => currentDepthEntities.Contains(r.SourceEntity_Id) || currentDepthEntities.Contains(r.TargetEntity_Id)) // remove .Include after that
                .ToListAsync();
    ```
    6. Redundant Global Query Filter per `AssetEntity`
    ```csharp
    // The 1st
    modelBuilder.Entity<AssetEntity>().HasQueryFilter(e => !e.IsDeleted);
    ```
    ```csharp
    // The 2nd
    modelBuilder.Entity<AssetEntity>().HasQueryFilter(e => !e.IsDeleted && (e.Tenant_Id == _currentTenantId || _currentTenantId == null));
    ```
    ==> **Fix**: Remove the 1st and keep the 2nd
    7. Minor bugs: 
      - Correct remaining `DateTime.Now` usage to `DateTime.UtcNow`
      - Fix logical incorrectness in seed data: Total OwnershipPercentage of asset realstate1 > 100%
      - Change finance-related amount type from `float` to `decimal` for precision preservation
    8. Improvements I want to implement if time is allowed:
      - Exception is not being processed (except Json missing keyword exception is handled in POST) and is thrown as 500 with full stacktrace which might leak system internal information to mischievous user (hacker) => I'd like to implement a general try catch in each controller, and translate well exception to HTTP error while hiding sensitive information. I prepared `Error.cs` for error extension
      - The `GetIndirectRelationshipsAsync(Guid entityId, int depth)` is using BFS-like loop to expand the indirect relationship, but there is no visited node check. Therefore there is chance the relationships found are duplicated. If time permits, I'd like to implement the de-deduplication for this traversal

---

### Part 2: Feature Implementation

Pick **2** of the following features and implement them. For the ones you don't implement, briefly describe your approach.

1. **Pagination**: The `GET /api/AssetEntity` endpoint to return all entities and add pagination support with query parameters.

2. **Search/Filter**: Add a way to filter entities by `EntityType`, `RiskLevel`, or `Tags`.

3. **Ownership Validation**: When creating an `AssetOwnership`, validate that the total ownership percentage for a given asset does not exceed 100%.

4. **Relationship Delete**: Add a `DELETE /api/AssetEntity/relationships/{id}` endpoint that properly handles soft-deletion.

**Write your approach/notes here:**
I picked 1,2,4 to implement
1. **Pagination**
- Approach:
  - Create `PaginationParameters` class to capture 2 query parameters `page` (default = 1 as the 1st page if omitted)and `pageSize` (null if omitted). This class instance will be generated via `[FromQuery] PaginationParameters pagination` in Controller method `GetAssetEntities`
  - The `PaginationParameters` object is passed down to `Service` layer -> `Repository` layer at method `GetAllAsync(...)`
  - As `page` is not null, if `pageSize` is provided, the list of entities is
  ```csharp
  var query = _context.AssetEntities.AsQueryable();
  ...
  if (pagination.PageSize.HasValue)
  {
    query = query
    .Skip((pagination.Page - 1) * pagination.PageSize.Value)
                  .Take(pagination.PageSize.Value);
  }  
  return await query.ToListAsync();
  ```
  If `pageSize` is omitted, we can simply return all entities

  - The pattern 
  ```csharp
  var query = _context.AssetEntities.AsQueryable();
  ...
  // Other filtering actions chained in query
  ...  
  return await query.ToListAsync();
  ```
  enables composable filters, as we see in implementation of filter in 2/ below
- Integration tests: 2 OK cases
  - 1 case seeding 2 entities uses `page=1&pageSize=1` and `page=2&pageSize=1`, then asserts 2 pages captures 2 Ids
  - 1 case seeding 2 entities uses `page=1` and omits `pageSize`, then asserts return all 2 entities
2. **Search/Filter**
- Approach:
  - Similar to `Pagination` above, I provide filter via query parameters using separate class `AssetEntityFilterParameters` with fields : `EntityType`, `RiskLevel`, `Tag`. The API query then supports `GET /api/AssetEntity?entityType=xxx&riskLevel=yyy&tag=zzz`. 
    > Note:
    > - `tag=zzz` will return entities having `one of tags` as `zzz`.
    > - keyword is case-insensitive, while value is case-sensitive
  - Then the implementation is straightforward in `AssetEntityRepository.cs`, thanks to composability of the query
  ```csharp
  var query = _context.AssetEntities.AsQueryable();

  if (!string.IsNullOrEmpty(filter.EntityType))
  {
      query = query.Where(e => e.EntityType == filter.EntityType);
  }

  if (!string.IsNullOrEmpty(filter.RiskLevel))
  {
      query = query.Where(e => e.RiskLevel != null && e.RiskLevel == filter.RiskLevel);
  }

  if (!string.IsNullOrEmpty(filter.Tag))
  {
      query = query.Where(e => e.Tags != null && e.Tags.Contains(filter.Tag));
  }
  ...
  return await query.ToListAsync();
  ```
  > Note:
  > - The `EntityType` and `RiskLevel` searchs can be very quick when moving to Database and in large scale, because they can leverage DB indexing
  > - On contrary, `Tag` search uses `e.Tags.Contains` cannot leverage indexing, nor SQL translation. It has to convert the serialized data from DB to build the list then apply `contain`, so it involves full DB scan in worst case => Should warn user in case of big dataset
- **Future improvement**: I anticipate if we'd like to filter case-insensitive. To guarantee equivalent performance in real production DB, we can do 2 actions: Normalize searched fields (e.g. `EntityType`) to always lowercase when saving to DB then adapt condition from `e.EntityType == filter.EntityType` to `e.EntityType == filter.EntityType.toLower()`. As `filter.X.toLower()` is converted to literal constant at query time, so the indexing mechanism is applied wherever possible
- Integration tests: 3 OK cases, each per filter type
3. **Ownership Validation**
- I don't implement this feature, but can summarize my approach
- Approach:
  - Input: `AssetOwnership newOwnership`
  - We add this check in `Service` method that will do the createOwnership
  ```csharp
  var existingOwnerships = await _repository.GetOwnershipsByAssetIdAsync(newOwnership.Asset_Id);

    var totalExisting = existingOwnerships.Sum(e => e.OwnershipPercentage);
    var newTotal = totalExisting + newOwnership.OwnershipPercentage;

    if (newTotal > 100)
    {
        throw new InvalidOperationException(
            $"Total ownership percentage could not exceed 100%. Current: {totalExisting}%, Requested: {newOwnership.OwnershipPercentage}%.");
    }
    // Create only if total <= 100%
    await _repository.CreateOwnership(newOwnership);
  ```
  - `Note`: The code is working for EF in memory, with real production DB, we will need pessmistic locking `System.Data.IsolationLevel.Serializable` pattern to avoid 2 POST requests updating at the same time and breaking total percentage, as below
  ```csharp
  using var transaction = await _context.Database.BeginTransactionAsync(
        System.Data.IsolationLevel.Serializable);
  try {
    ...
    // total check above
    ...
    await _repository.CreateOwnership(newOwnership);
    await transaction.CommitAsync();
  } catch {
    await transaction.RollbackAsync();
    throw;
  }
  ```
4. **Relationship Delete**
- Approach
  - Add new endpoint method `DeleteRelationship(Guid relationshipId)` for path `relationships/{relationshipId}` under `/api/AssetEntity`
    - This method searches for any relationship with such Id, and call `Service` to Delete this relationship
    - To facilitate `soft-delete`, we need to uncomment the Global Query Filter for Relationship entity in `AssetManagementDbContext.cs`
    ```csharp
    modelBuilder.Entity<Relationship>().HasQueryFilter(r => !r.IsDeleted);
    ```
    Then, all queries will not display Relationship `isDeleted = true` anymore
    - As `.FindAsync` might bypass Global Query Filter in real production DB, I change the `.FindAsync` in `GetRelationshipByIdAsync` to
    ```csharp
    _context.Relationships.FirstOrDefaultAsync(r => r.Relationship_Id == relationshipId);
    ```
    - Finally the `DeleteRelationship` method is already applying pattern of hard `.Remove()`, then intercepting in `.SaveChangeAsync()` subsequently and converting this hard deletion to `Modify` and `IsDeleted = true`
- Integration tests: 1 OK soft-deletion case, 1 KO Not Found relationshipId
---

### Part 3: Architecture & Design Questions

Answer the following questions with a small paragraph. You don't need to write code for these. There are no trick questions, again, it's only to dig through your brain.

**Q1**: The application uses an in-memory database. What would change if you had to switch to a real database (SQL Server or IBM DB2)? What problems might appear that are currently hidden?
- With in-memory code, we don't care about concurrency, with real production DB, we should pay attention on implementing lock mechanism in cases such as guarding from AssetOwnership percentage simultaneous update > 100 %
- In real Database, the `.FindAsync` might bypass the Global Query Filter that hide soft-deleted entities, so it can expose deleted data which is security risk. Therefore, we should replace it by `.FirstOrDefaultAsync` which always applies Global Filter
 

**Q2**: The `SaveChangesAsync` override in the DbContext handles soft-deletion and audit logging. What do you think about this approach? What would you change or improve?
- At first, this approach looks not intuitive to me, I was wondering why we should not simply set `<Object>.IsDelete = true`. Then I found it has several pros:
  - If setting directly, I have to duplicate this code in every Delete method. It also does not scale, new class is introduced, this Delete has to be replicated for this class. Meanwhile, unifying soft-deletion in `SaveChangeAsync`, the processing is centralized and with new class, the call is invariant `DbContext.SaveChangeAsync`
  - When the deletion triggers other actions systematically (e.g. Audit Logging in this case), having a single source of truth like this is very helpful at scale as it applies for any class.
- Improve:
  - Silent catch {} in audit logging swallows exceptions with no logging — errors are lost for investigation => we can at least log the error
  - The string Changes in AuditLog is storing the value after modification, it is more visualized if we log `entry.Properties.OriginalValue` (old value) and `entry.Properties.CurrentValue` (new value), in case of `Added`, the old value is `null`
  - I saw `PreventModificationOfLockedEntities` is defined but never called. That logic is helpful, and should be put prior to the soft-deletion logic so that it can stop early without any change => I have integrated into `SaveChangeAsync`


**Q3**: The application has a `TenantResolutionMiddleware` that reads a tenant ID from the `X-Tenant-Id` HTTP header. Try calling the API with and without this header. What do you observe?
- With X-Tenant-Id header: `dbContext.SetTenant(tenantId)` is called => _currentTenantId is set => the query filter `e.Tenant_Id == _currentTenantId` is active => only that tenant's entities are returned.
- Without the header: `dbContext.SetTenant(tenantId)` is not called => _currentTenantId is null => the filter condition || _currentTenantId == null is true for every row => all tenants' data is returned. This is a security issue: missing the header leaks cross-tenant data

**Q4**: Look at the `GetIndirectRelationshipsAsync` method in the repository. It goes through entity relationships using a loop. What problems might or will happen if the dataset grows to millions of records? How would you improve it?
- Problems:
  -  After each loop round, the `currentDepthEntities` is expanded with all entities that has relationship with one of them. Furthermore, there is no cycle detection applied. So the `currentDepthEntities` might tend to grow exponentially. With dataset ~ million rows, probably after 4-5 rounds, the length of  `currentDepthEntities` could reach ~ million records in memory. Besides in the next round, the `.Where(r => currentDepthEntities.Contains(r.SourceEntity_Id) || currentDepthEntities.Contains(r.TargetEntity_Id))` is translated to a `WHERE ... IN (...)` with IN clause could reach thousands quickly, that could hit performance downgrade zone
- Migitation:
  - As mentioned, we can alleviate a little by remembering visited entity, to avoid cycle loop and restrict more slowly the growth of new entity in `currentDepthEntities`
  - If we're using SQL DB, this graph-like path building can be executed at DB side using feature `recursive CTE`. We can do a single DB query, rather than `depth` round-trip to DB, and furthermore takes advantage of DB engine. That would be more robust approach at large scale of million records

**Q5**: If you had to add a feature to track the **full history** of every changes made to relationships (who changed what, when, old value vs new value), how would you design it?
- My approach:
  - Inspired from an improvement in `SaveChangeAsync` above, I realize: 
    - The current AuditLog can refactor to log `(old value, new value)` in Changes, thanks to `entry.Properties`. 
    Besides, AuditLog already has `When` (Timestamp).
    - `Who change` is the API caller, which should be enriched in AuditLog as a requirement after Authentication and Authorization part is complete
    - `What` can be `EntityId`
    - Lastly, AuditLog capture changes of all `CommonBase`, including `Relationship`, the type of object, i.e. `Relationship` can be filtered from `EntityName` field in `AuditLog table`
  - Therefore, after little refactoring, we can retrieve **full history** of relationship changes by simply query `EntityName == "Relationship"` in `AuditLog table`. Definitely the indexing of this column will help optimize the query.
  - A core repository method can be built 
  ```csharp
  public async Task<IEnumerable<AssetEntity>> GetFullRelationshipHistoryAsync(){
    return await _context.AuditLogs
    .Where(a => a.EntityName == "Relationship")
    .OrderByDescending(a => a.Timestamp)
    .ToListAsync();
  }
  ```
  - Then controller endpoint and service to exploit this method are business as usual

---

### Part 4: Production Scenario

You don't need to write code for these either, just explain how you would investigate and solve the problem.

**Scenario A**: A user reports that when they update a LegalEntity's `LegalName` via `PUT /api/AssetEntity/{id}`, the response is `204 No Content` (success), but the name hasn't actually changed. The `UpdatedAt` timestamp IS updated. Walk through how you would debug this.
- As I faced this bug already in Part 1 and fixed it, let me explain investigation steps:
  - The API finishes with success and at least 1 field is updated, every step Controller -> Service -> DB has no issue, it just updated partially
  - I then check each layer to see where the data is updated. Then I can find at `Service` layer the method in charge of `UpdateEntityAsync` is updating some common fields of `AssetEntity` (including `UpdatedAt`), but not `LegalName` nor Natural specific fields either => The problem is found
- Analysis toward solution:
  - We are having existingEntity retrieved from DB with correct type (Legal or Natural), our goal is to update each field that existingEntity has if it is provided in PUT request
  - With this requirement, we should not use `AssetEntity` as request type anymore because it implies deserialization to `AssetEntity` or its sub-types, which will potentially lose information anyway
  - So a proposal is creating a dedicated DTO for this PUT request that contains all possible fields of sub type of `AssetEntity`, thus no information is lost after JSON deserialization => Then we just do mapping each field of `existingEntity` after casted to correct sub-type to relevant field if not null in deserialized DTO

**Scenario B**: After deploying to production, you get reports that deleting an entity sometimes causes other, seemingly unrelated entities to "disappear" from the API. What could cause this? Same as before, walk through how you would debug this.
- Invesigate:
  - From the phenomenon's description: the deletion of an entity links to other entities'disapperance, I will think of `cascade on deletion`
  - I then go to `AssetManagementDbContext` to check, and find out some entities actually have `DeleteBehavior.Cascade` relations together, e.g.
    - `AssetEntity` -> `AssetOwnership`
    - `Asset` -> `AssetOwnership`
    - `AssetEntity` -> `EntityPosition`
  - Looking above cascading, I can conceive a scenario probably leading to such phenomenon, e.g.
    - User A is viewing `GET /api/AssetEntity/idA/AssetOwnership/M`
    - `AssetOwnership M` linked to `Asset P`
    - User B then deletes `Asset P` via `DELETE /api/Asset/P`
    - This deletion triggers `cascade` deletion on `AssetOwnership M`
    - After `M` is deleted, user A might refresh the GET and see empty page, though he might think it is problem of `AssetEntity idA`
    > Note: the cascading is `Hard delete`, that bypasses our soft-deletion mechanism and ChangeTracker, so even there is no AuditLog. This behavior is therefore very sneaky, and should be considered to change to `DeleteBehavior.Restrict`.


**Scenario C**: A new developer adds a `Comment` entity to the project following the same patterns. They can save comments successfully, but when they query for them, the API always returns an empty list. What might be going on?
- Investigate:
  - The POST is returning OK, meaning the saving to DB is done
  - The GET `/Comment` will normally return all `Comment` **after** applying `Global Filter` => I will check if the Global Filter has any incorrectness
  - At original repo, we can see the pattern of Global Filter is filtering `entity.isDeleted = true && entity.tenantId == currentTenantId`, we can think of tenantId might link to the issue
  - Check back to the POST endpoint, the code replicates the same problematic pattern: the `tenantId` is saved into HttpContext, but never persisted into `AssetEntity` before calling repository to save into DB => So by default the `entity.tenantId` is always `null` although we shoot POST request with tenantId correctly => As a consequence, the GET afterwards with correct tenantId header will be used by Global Filter and no match will any new record in DB (having `tenantId` `null`)
  - If we can access DB production, we can confirm the Comments are saved and `tenantId` indeed `null`
  - We can reproduce in local to be sure to deliver a fix:
    - Comment out the Global Filter
    - Shoot POST request with correct tenantId
    - Shoot GET request with the same tenantId or without tenantId => we can see all records of `Comment` having `null` `tenantId`
    => As it is reproduced in local, we can raise PR to fix the bug (passing tenantId in HttpContext to object model before persisting)

---

Good luck!
