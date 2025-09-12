# MiniXafApi

This project demonstrates a DevExpress XAF Web API service, designed for rapid development and deployment of business applications. A key feature of this setup is its utilization of **local NuGet packages**, streamlining dependency management within the project.

## Features

*   **DevExpress XAF Web API:** A robust backend service built with the DevExpress eXpressApp Framework (XAF), providing a powerful and flexible platform for data-driven applications.
*   **Local NuGet Package Integration:** Dependencies are managed via local NuGet packages, simplifying the build process and ensuring consistent environments.
*   **Ready for Development:** Clone and run to quickly get started with an XAF Web API service.

## Getting Started

To get a local copy up and running, follow these simple steps.

### Prerequisites

*   .NET SDK (compatible with the project's target framework, likely .NET 6 or newer)
*   DevExpress Universal Subscription (required for XAF development)

### Installation

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/antonylu0826/MiniXafApi.git
    ```

2.  **Navigate to the project directory:**

    ```bash
    cd MiniXafApi
    ```

3.  **Restore NuGet packages:**
    Since this project uses local NuGet packages, ensure your NuGet configuration (`nuget.config`) points to the `nuget-packages` directory. The provided `nuget.config` in the repository should handle this automatically.

    ```bash
    dotnet restore
    ```

4.  **Build the solution:**

    ```bash
    dotnet build
    ```

5.  **Run the application:**

    ```bash
    dotnet run --project MiniXafApi.WebApi
    ```

    The API service should now be running, typically accessible at `https://localhost:5001` or `http://localhost:5000`.

## Project Structure

*   `MiniXafApi.WebApi/`: The main XAF Web API project.
*   `nuget-packages/`: Directory containing local NuGet packages used by the project.

---
