# Comments App

This repository contains the Comments App, which includes a server application, a user interface (UI), and a load testing tool. 
The application allows users to interact with comments by adding new ones, replying to existing ones, and uploading images. 
It features sorting, pagination, lazy loading, and integrates with various technologies like Kafka, Redis, and AWS S3.

## 1. Prerequisites

- **Docker**
- **Docker Compose**
- **Visual Studio** (optional, for Option 2)
- **Git**

## 2. Installation

### 2.1 Cloning the Repositories

#### 2.1.1 Clone the main server repository:

```bash
git clone https://github.com/YourUsername/comments-app.git
2.1.2 Clone the UI repository:
bash
Copy code
git clone https://github.com/GodLikeCSharpDeveloper/comments-app-UI.git
2.2 Setting Up the Server
Option 1: Running via Docker Compose
Navigate to the server directory:

bash
Copy code
cd comments-app/server
Copy the settings.json file:

Extract the contents of the settings.json archive.
Place the settings.json file into comments-app/server/Comments-app.
Run Docker Compose:

bash
Copy code
docker-compose up
Access the server:

The server will be running at http://localhost:8080.
Option 2: Running via Visual Studio
Open the solution:

Open CommentsApp.sln located in comments-app/server using Visual Studio.
Run Docker Compose from Visual Studio:

Set the docker-compose project as the startup project.
Start debugging by pressing F5.
Test the API:

Swagger UI should automatically open, allowing you to test the API endpoints.
2.3 Setting Up the UI
Navigate to the UI directory:

bash
Copy code
cd comments-app-UI
Run Docker Compose:

bash
Copy code
docker-compose up
Access the UI:

The UI will be available at http://localhost:4200.
Interact with the Application:

Home Page (/home): Features a table with sorting and pagination.
Comments Page (/comments): Allows adding comments or replying, with image and text uploads.
Ensure Connectivity:

The UI communicates with the server at http://localhost:8080.

2.4 Running NUnit Tests

Option 1: Using the Command Line

Navigate to the Tests Directory:

bash
cd comments-app/server/Tests
 

Restore Dependencies:

bash
dotnet restore


2.5 Run the Tests:

bash
dotnet test


This will execute all NUnit tests for the server application.

Option 2: Using Visual Studio

Open the Solution:

Open the `CommentsApp.sln` file located in `comments-app/server` using Visual Studio.

Locate the Test Project:

In the Solution Explorer, find the test project, for example, `CommentsApp.Tests`.

Run the Tests:

Open Test Explorer by navigating to `Test > Test Explorer` in the top menu.
Click on Run All to execute all NUnit tests.

This allows you to run and debug NUnit tests directly within the Visual Studio environment.


3. Running the Load Tester
Navigate to the Load Tester directory:

bash
Copy code
cd comments-app/LoadTester
Configure the Load Tester:

Adjust the concurrent and total requests in the configuration file as needed.
Run Docker Compose:

bash
Copy code
docker-compose up
Start Testing:

The Load Tester will begin stress-testing your server.
4. Interacting with the Application
4.1 Features Implemented in the UI:
Lazy loading
Pagination
Google reCAPTCHA V3 integration
4.2 Features Implemented in the Server:
Background services for task processing
Kafka for comment queuing
Redis for data storage and queue management
Entity Framework with bulk operations for database interactions
5. Monitoring with Grafana and Prometheus
Access Grafana:

Open http://localhost:3000 in your web browser.
Log In:

Username: admin
Password: admin
Skip Password Change:

You can skip changing the password on the first login.
Add Data Source:

Navigate to Configuration > Data Sources.
Click Add data source.
Select Prometheus.
Set the URL to http://prometheus:9090.
Click Save & Test to confirm the connection.
Import Dashboard:

Click on Dashboards in the left panel.
Click New and select Import.
Upload kafkadashboard.json from comments-app/LoadTester/kafkadashboard.json.
This dashboard will display Kafka metrics related to your application.
6. Technologies Used
6.1 Server Application:
ASP.NET Core
Kafka
Redis
Entity Framework (with bulk operations)
AWS S3
Background services for file processing
6.2 User Interface:
Angular
Lazy loading
Pagination
Google reCAPTCHA V3
6.3 Load Testing and Monitoring:
Custom Load Tester application
Grafana
Prometheus
7. Notes
Db schema is located at comments-app\server\dbModel.mwb
Ensure all necessary configurations, such as AWS credentials and Kafka settings, are correctly set before running the applications.
The Load Tester is designed for stress testing; use it responsibly to avoid unintended consequences.
The application is designed to be modular; you can run the server and UI separately if needed.
```
