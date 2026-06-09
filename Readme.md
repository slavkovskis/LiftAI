
# LiftAI

LiftAI is a modern, AI-powered personal training assistant designed to help users create and manage their workout routines. It leverages a Blazor WebAssembly frontend and a .NET backend, with AI features powered by a local Ollama instance.

## Features

*   **AI-Powered Chat**: Interact with an AI assistant to get personalized workout advice and generate new exercises.
*   **Workout Management**: Create, view, and manage your custom workout plans.
*   **Exercise Library**: Browse and manage a library of exercises.
*   **User Authentication**: Secure user registration and login system.
*   **Freemium Model**: Built-in limitations for free users with the flexibility to expand.

## Technology Stack

*   **Backend**: .NET 8, ASP.NET Core Web API
*   **Frontend**: Blazor WebAssembly
*   **Database**: Entity Framework Core with SQL Server (or other provider)
*   **Authentication**: JWT (JSON Web Tokens)
*   **AI**: Ollama (running a model like Llama 3.1)
*   **Real-time Communication**: SignalR (for chat)

## Getting Started

Follow these instructions to get the LiftAI solution up and running on your local machine for development and testing purposes.

### Prerequisites

*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   An IDE like [Visual Studio](https://visualstudio.microsoft.com/), [JetBrains Rider](https://www.jetbrains.com/rider/), or [VS Code](https://code.visualstudio.com/).
*   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or another database compatible with Entity Framework Core).
*   [Ollama](https://ollama.com/) installed and running with a model pulled (e.g., `ollama run llama3.1`).

### 1. Configuration

Before running the application, you need to configure your secrets and settings. The backend project (`LiftAI.Api`) uses `appsettings.json` for configuration. It's highly recommended to use the .NET Secret Manager for sensitive data.

**1.1. Initialize User Secrets**

Open a terminal in the `LiftAI.Api` directory and run:

```bash
dotnet user-secrets init
```

**1.2. Add Secrets**

Now, add the following secrets. Replace the placeholder values with your actual configuration.

*   **Database Connection String**:
    ```bash
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_DATABASE_CONNECTION_STRING"
    ```
    *(Example: `"Server=localhost;Database=LiftAI;Trusted_Connection=True;TrustServerCertificate=True;"`)*

*   **JWT Secret Key**: This should be a long, random, and secret string.
    ```bash
    dotnet user-secrets set "Jwt:Key" "YOUR_SUPER_SECRET_JWT_KEY_THAT_IS_LONG_AND_COMPLEX"
    ```

*   **SMTP Settings for Email (Optional)**: Required for features like email confirmation.
    ```bash
    dotnet user-secrets set "Smtp:Password" "YOUR_SMTP_PASSWORD"
    ```
    *(Note: The SMTP User is pre-configured as `liftai.noreply@gmail.com` in `appsettings.json`. You can override it here if needed.)*

**1.3. Verify Ollama Configuration**

Ensure the Ollama URL in `LiftAI.Api/appsettings.json` matches your local setup. The default is `http://localhost:11434`.

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.1",
  "TimeoutSeconds": 120
}
```

### 2. Database Setup

The application uses EF Core migrations to manage the database schema.

**2.1. Apply Migrations**

When you run the `LiftAI.Api` project for the first time, it will automatically apply all pending migrations and seed the database with default exercises.

Alternatively, you can apply them manually from the terminal in the `LiftAI.Api` directory:

```bash
dotnet ef database update
```

### 3. Running the Application

To run the solution, you need to start both the backend API and the frontend Blazor application.

*   **LiftAI.Api**: The ASP.NET Core backend.
*   **LiftAI.App**: The Blazor WebAssembly frontend.

**Using an IDE (Visual Studio / Rider):**

1.  Set up a **compound run configuration** that starts both `LiftAI.Api` (using the `http` profile) and `LiftAI.App`.
2.  Launch the configuration. Your browser should open to the Blazor app, and the API will be running in the background.

**Using the .NET CLI:**

1.  **Start the API**:
    Open a terminal in the `LiftAI.Api` directory and run:
    ```bash
    dotnet run --launch-profile http
    ```
    The API will be available at `http://localhost:5250`.

2.  **Start the App**:
    Open a *second* terminal in the `LiftAI.App` directory and run:
    ```bash
    dotnet run
    ```
    The Blazor app will be available at `http://localhost:5187`.

3.  Open your browser and navigate to `http://localhost:5187`.

You can now register a new user and start testing the application.

## Project Structure

The solution is organized into three main projects:

*   `LiftAI.Api`: The backend server. It handles business logic, database access, user authentication, and serves the AI chat endpoints.
    *   `/Endpoints`: Defines the API routes.
    *   `/Data`: Contains the `DbContext`, models, and migrations.
    *   `/Services`: Houses services for chat, email, etc.
    *   `appsettings.json`: Main configuration file.

*   `LiftAI.App`: The frontend client. A Blazor WebAssembly application that runs in the user's browser.
    *   `/Pages`: Contains the main routable components/pages.
    *   `/Components`: Reusable UI components.
    *   `/Auth`: Handles JWT authentication state.
    *   `/wwwroot`: Contains static assets, including `appsettings.json` for the client.

*   `LiftAI.Shared`: A shared class library for data transfer objects (DTOs) and models used by both the API and the App to ensure consistency.

