# Asset Management System README

## Overview

This project is an Asset Management System developed as a C# Web API using .NET 8. It includes the ability to manage various asset entities, such as legal and natural persons, as well as concrete assets like real estate, stocks, and intellectual property.

The system is designed to help you understand and manage complex relationships between entities, assets, and the metadata associated with them. The project also integrates various practices to ensure data consistency, business logic encapsulation, and a clean architecture using repositories, services, and controllers.

## Technical Overview

The project is composed of the following layers:

- **Models** - Defines the data structure for asset entities (`AssetEntity`, `LegalEntity`, `NaturalEntity`), assets (`RealEstate`, `Stock`, `IPAsset`), and relationships between them.
- **DbContext** - `AssetManagementDbContext` manages the database connection and handles the Entity Framework Core operations for the project.
- **Repositories** - Handles the data access logic. The `AssetEntityRepository` performs CRUD operations for asset entities.
- **Services** - Business logic layer. The `AssetEntityService` coordinates actions between controllers and the data access layer.
- **Controllers** - Defines the Web API endpoints for managing asset entities via the `AssetEntityController`.

# Candidate Exam Questions
## Part 1: Code Comprehension
### Service and Repository Analysis

- Review the provided `AssetEntityService` and `AssetEntityRepository` files.
  - Explain the purpose of the `UpdateEntityAsync` method in the `AssetEntityService` and describe the key checks implemented before updating an entity.
  - How would you modify this method to handle scenarios where certain fields should not be updated by default (e.g., `CreatedAt`, `EntityType`)?

- Analyze the CreateOrUpdateRelationshipAsync method:
  - Describe how circular relationships are prevented during the creation of a relationship.
  - Explain the role of the CheckCircularRelationshipAsync method and how it ensures relationship integrity.

### Model Relationships

- Analyze the `AssetEntity`, `LegalEntity`, and `NaturalEntity` classes.
  - Describe how inheritance is used to handle different entity types (legal and natural entities).
  - How are relationships between entities managed in the data model? Provide an example of how a `Relationship` would be created between two entities.

## Part 2: Implementing New Features
### Feature Development Task

- Batch Update Risk Level:
  - Add a new feature to the `AssetEntityService` that allows batch updating the risk level for multiple asset entities. The input should be a list of entity IDs and a risk level value. Consider:
	- Adding a new service method `BatchUpdateRiskLevel`.
	- Implementing the corresponding repository method to perform the bulk update using Entity Framework Core.
	- Describe how you would ensure that the operation is safe in terms of concurrency.

- Indirect Relationships Query:
  - Implement a feature in the `RelationshipService` that allows querying indirect relationships of an entity up to a certain depth.
  - Write the method `GetIndirectRelationshipsAsync` to return indirect relationships up to a specified depth.
  - Describe how to optimize these recursive queries for performance using Entity Framework.

### Entity Validation

- Implement validation in `AssetEntityRepository` to ensure that:
  - An `AssetEntity` cannot have the same `EntityReference` as an existing entity.
  - Update the `AddAsync` and `UpdateAsync` methods to add these checks before persisting data to the database.

## Part 3: Extending and Improving Existing Logic
### Refactoring for Extensibility
#### Refactor GetAllAsync for Pagination:

- Refactor the `GetAllAsync` method in `AssetEntityRepository` to support pagination. Update the `AssetEntityController` to allow clients to request specific pages of asset entities with a given page size.
  - Implement the changes in both the repository and controller.
  - Consider performance and usability aspects of the pagination feature.

#### CheckCircularRelationshipAsync Refactoring:

- Refactor the `CheckCircularRelationshipAsync` method for scalability:
  - Suggest changes to make this method efficient when there is a large number of relationships.
  - Explain how to handle potential performance bottlenecks with graph-like data traversal.

### Data Integrity Improvement
#### Delete Entity and Related Data:

- In the `DeleteEntityAsync` method of the `AssetEntityService`, ensure that all related `Relationships` and `Positions` are also deleted or handled appropriately when an entity is deleted.
  - Describe how you would implement cascading deletes or how to handle orphaned records using Entity Framework Core.

## Part 4: Debugging and Troubleshooting
#### Concurrency Handling

- Analyze the `AssetEntityService` and `AssetEntityRepository` classes:
  - How would you ensure that the `UpdateEntityAsync` method is safe from concurrency issues if two clients try to update the same entity simultaneously?
  - Suggest an approach to manage optimistic concurrency in this context.
- Given the new `CreateOrUpdateRelationshipAsync` method, describe how you would implement optimistic concurrency control using Entity Framework's concurrency tokens.

#### Bug Fixing

- Suppose you encounter a `NullReferenceException` in the `GetEntityByIdAsync` method of the `AssetEntityService` when certain related entities are missing.
  - How would you modify the repository method to handle null values more gracefully?
  - What changes would you make to the controller to ensure a consistent and user-friendly response?

## Part 5: Testing and Integration (Optional)
### Integration Task
- Integrate the `AssetEntity` system with the relationship context to handle asset ownership hierarchies.
  - Modify the `AssetEntityService` to include methods that retrieve all assets owned by entities linked via relationships.
  - Write integration tests that validate ownership retrieval logic, ensuring that ownership propagates through relationship layers.

### Testing Strategy
- Describe your approach to unit and integration testing for the `RelationshipService`.
  - What testing tools or frameworks would you use?
  - Write a unit test for the `GetRelationshipByIdAsync` method that mocks dependencies and validates different outcomes (e.g., relationship found, relationship not found).

## Running the Application
To run the application, follow these steps:

1. **Prerequisites**: Ensure you have .NET 8 SDK installed.
2. **Build and Run**:
   - Use the command `dotnet build` to build the application.
   - Run the application using `dotnet run`.
   - The application uses an in-memory database (`AssetManagementTestDb`) configured in `Program.cs` for easy testing.
3. **API Testing**: You can test the API using Swagger, which is available at `https://localhost:{port}/swagger` when running the application in development mode.

# Good luck!
