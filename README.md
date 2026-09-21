# Rental Property Management

A web app for residential rentals. An Applicant picks an available unit and applies through a step-by-step wizard; a Property Manager runs the buildings and units and decides on applications: Approve, Return, or Deny. An approved unit gets a 12-month Lease and leaves the available list.

Repository: [upworkdesignerben/rental-property-management](https://github.com/upworkdesignerben/rental-property-management).

## Run with Docker

You need Docker Desktop and Docker Compose. No local .NET SDK required.

```powershell
Copy-Item .env.example .env
docker compose up --build -d
```

Open **http://localhost:8080**. On the first start the app creates the database, applies migrations, and adds demo data by itself.

Stop without losing data: `docker compose down`.

## Demo users

| Role | Login | Password |
|---|---|---|
| Applicant | `applicant@demo.local` | `DemoPassword1!` |
| Property Manager | `manager@demo.local` | `DemoPassword1!` |

## Features

**Applicant**
- Browse available units and view unit details.
- Create an application for an available unit.
- Fill the application wizard: Applicant Information → Residence History → Summary.
- Save each section with Continue, go Back without saving, Submit from the Summary.
- Add, edit, and delete residences in a modal.
- Edit Draft and Returned applications, resubmit after a Return.
- Withdraw an application; track its status in My applications.

**Property Manager**
- Create, edit, and delete Properties and Units in modals.
- Inactive Unit Types stay on their units but cannot be assigned to new ones.
- See all applications with Status and Property filters.
- Open any application: sections, status history, reviews.
- Review Submitted applications in a modal: Approve, Return, Deny.
- A comment is required for Return and Deny.
- Approval creates a 12-month Lease; the unit becomes unavailable.

## How it is built

The app uses ASP.NET Core MVC with Razor: thin controllers, a ViewModel per page and form, Razor views, partial views, and view components. The Property, Unit, Residence, and Applicant Information forms are partials; the read-only application Summary is built by the `ApplicationSummaryViewComponent`.

Modals are filled with partial views returned by controller actions. If the posted form fails validation, the server returns the same partial with error messages — the modal stays open. On success it returns OK, the modal closes, and the page refreshes only the affected fragment.

An application is one page, one form, one POST: the Back, Continue, and Submit buttons set the command, and the server picks the step. Back drops the input, Continue checks and saves only the current section, Submit works only from the Summary after both sections are saved. Approval creates a Lease of exactly 12 months in a single transaction — a second approval of the same unit is rejected, so a double Lease is impossible.

Business rules check: `dotnet test PropertyRental.sln` (73 unit tests, no database needed).
