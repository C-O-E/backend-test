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

> *(Your answer)*

---

### Part 2: Feature Implementation

Pick **2** of the following features and implement them. For the ones you don't implement, briefly describe your approach.

1. **Pagination**: The `GET /api/AssetEntity` endpoint to return all entities and add pagination support with query parameters.

2. **Search/Filter**: Add a way to filter entities by `EntityType`, `RiskLevel`, or `Tags`.

3. **Ownership Validation**: When creating an `AssetOwnership`, validate that the total ownership percentage for a given asset does not exceed 100%.

4. **Relationship Delete**: Add a `DELETE /api/AssetEntity/relationships/{id}` endpoint that properly handles soft-deletion.

**Write your approach/notes here:**

> *(Your answer)*

---

### Part 3: Architecture & Design Questions

Answer the following questions with a small paragraph. You don't need to write code for these. There are no trick questions, again, it's only to dig through your brain.

**Q1**: The application uses an in-memory database. What would change if you had to switch to a real database (SQL Server or IBM DB2)? What problems might appear that are currently hidden?

> *(Your answer)*

**Q2**: The `SaveChangesAsync` override in the DbContext handles soft-deletion and audit logging. What do you think about this approach? What would you change or improve?

> *(Your answer)*

**Q3**: The application has a `TenantResolutionMiddleware` that reads a tenant ID from the `X-Tenant-Id` HTTP header. Try calling the API with and without this header. What do you observe?

> *(Your answer)*

**Q4**: Look at the `GetIndirectRelationshipsAsync` method in the repository. It goes through entity relationships using a loop. What problems might or will happen if the dataset grows to millions of records? How would you improve it?

> *(Your answer)*

**Q5**: If you had to add a feature to track the **full history** of every changes made to relationships (who changed what, when, old value vs new value), how would you design it?

> *(Your answer)*

---

### Part 4: Production Scenario

You don't need to write code for these either, just explain how you would investigate and solve the problem.

**Scenario A**: A user reports that when they update a LegalEntity's `LegalName` via `PUT /api/AssetEntity/{id}`, the response is `204 No Content` (success), but the name hasn't actually changed. The `UpdatedAt` timestamp IS updated. Walk through how you would debug this.

> *(Your answer)*

**Scenario B**: After deploying to production, you get reports that deleting an entity sometimes causes other, seemingly unrelated entities to "disappear" from the API. What could cause this?

> *(Your answer)*

**Scenario C**: A new developer adds a `Comment` entity to the project following the same patterns. They can save comments successfully, but when they query for them, the API always returns an empty list. What might be going on?

> *(Your answer)*

---

Good luck!
