---
name: outsystems-naming-conventions
description: Standard OutSystems naming conventions and folder structures for actions, entities, structures, variables, and UI elements. Use whenever creating or renaming elements in an OutSystems application.
---

# outsystems-naming-conventions

Consistent naming is critical for maintainability and readability across teams in OutSystems. AI models must adhere strictly to standard OutSystems naming conventions rather than generic programming styles.

---

## 1. Logic Actions

All Server Actions, Screen Actions, and Data Actions must follow **PascalCase** using the **`Verb + Noun`** formula:

### ✅ Good Names
* `Customer_GetById`
* `Order_ValidateDiscount`
* `Invoice_CalculateTotal`
* `User_SendWelcomeEmail`
* `File_ProcessUpload`

### ❌ Bad Names
* `check` (vague, missing noun)
* `doWork` (camelCase, vague verb)
* `customer_action` (snake_case)
* `data` (no verb, completely ambiguous)

---

## 2. Logic Folder Organization

Actions in the Logic tab must be neatly organized into folders:

* **`Public/`**: Actions exposed to other modules (`Public = Yes`).
* **`Internal/`**: Private helper actions restricted to this module.
* **`CRUD/`**: Standard entity create/read/update/delete wrapper actions.
* **`Validations/`**: Business validation logic actions (e.g. `Order_ValidateStock`).
* **`Sync/`**: Offline data synchronization actions (in Mobile/Reactive).

---

## 3. Data Entities and Attributes

* **Entities**: Singular, PascalCase noun (e.g. `Customer`, `Invoice`, `OrderItem`).
  * Never use plural names (`Customers`, `Invoices`).
* **Attributes**: PascalCase, descriptive and concise:
  * Identifiers: `<EntityName>Id` (e.g. `CustomerId`, `StatusId`).
  * Autonumber primary key: `Id`.
  * Date/Time attributes: Include context (e.g. `DueDate`, `SubmittedOn`, `ExpiresAt`).

---

## 4. Structures (DTOs)

Structures used for API payloads, component interfaces, or complex data types:
* **Prefix**: Standard prefix `ST_` followed by PascalCase (or clean domain name):
  * `ST_CustomerDetails`
  * `ST_PaymentRequest`
  * `ST_ApiResponse`
* Structures representing a list response can use `ST_<Name>List` or a dedicated item structure.

---

## 5. Variables and Parameters

### Boolean Variables
Booleans must always start with an interrogative prefix:
* `IsActive`
* `HasPermission`
* `CanEdit`
* `IsValid`
* `MustChangePassword`

❌ Never name a boolean `active`, `flag`, or `check`.

### Lists
Variables or outputs containing a list of records must end with `List`:
* `CustomerList`
* `ProductSummaryList`
* `SelectedIdList`

### Parameters
* **Input Parameters**: Clearly define what value is expected (e.g. `CustomerId`, `MaxResults`, `IncludeInactive`).
* **Output Parameters**: Clearly define what is returned (e.g. `IsSuccess`, `ErrorMessage`, `CreatedId`, `CustomerDetails`).

---

## 6. UI Widgets & Screens

* **Screens**: Clean descriptive nouns reflecting the business view:
  * `Customer_List`
  * `Customer_Detail`
  * `Order_Dashboard`
* **Reusable Blocks**: Prefix with block purpose or domain:
  * `Card_Customer`
  * `Modal_Confirmation`
  * `Widget_MetricKPI`
* **Buttons**:
  * Label: Action-oriented verb (e.g. `Save`, `Submit`, `Cancel`, `Download Report`).
  * Name in Widget Tree: `Btn_Save`, `Btn_Cancel`.
