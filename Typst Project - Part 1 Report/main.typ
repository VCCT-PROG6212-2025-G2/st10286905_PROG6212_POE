#import "@preview/grape-suite:3.1.0": exercise
#import exercise: project, task, subtask

#show: project.with(
    title: "PROG6212 POE Part 1",
    author: "Sky Martin",
    show-outline: true
)
#set text(lang: "en", region: "GB")

#pagebreak()
= Introduction
The Contract Monthly Claim System (CMCS) is a system for creating and reviewing contract claims. The system has three types of users: lecturers, program coordinators, and academic managers. Lecturers are able to create new claims and view the status of existing claims belonging to them. A program coordinator and an academic manager must both review a claim for it to be complete, with the claim only being approved if both reviewers accept it, otherwise it is rejected. The scope of part 1 of this project covers design choices, database structure, project plan, and a UI prototype which does not require working functionality.

= Design Choices

== Assumptions and Constraints
=== Assumptions
- User authentication is required
- Users have roles which define what they are allowed to do, with roles for lecturer, program coordinator, academic manager, and admin
- New users cannot do anything, and admins must assign users to relevant roles
- Lecturers teach modules.
- Program coordinators create and manage modules, assigning modules to lecturers.
- Each claim is for one module that the lecturer teaches.
- Each claim may optionally have multiple supporting documents.
- Each claim must be reviewed by a program coordinator and an academic manager to be complete, with the claim only being accepted if both agree else rejected.
- A lecturer may leave a comment when creating a claim, and reviewers may leave comments when reviewing a claim.
- The system assumes lecturers are honest when creating claims. It does not have any way to validate whether a lecturer really worked a specific number of hours, or is really paid a specific hourly rate.
=== Constraints
- Claims have one and only one module.
- One program coordinator review per claim, one academic manager review per claim.
- Uploaded documents for claims must be one of the following types: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, or .md.
- Lecturers may only view their own claims, and only download their own supporting documents.


== Code Structure
The system is implemented as an ASP.NET Core .NET 8 Model View Controller (MVC) project. The project makes use of Entity Framework Core for database integration and ASP.NET Core Identity for user authentication and role management. Following the MVC pattern, the project has multiple models, views, and controllers. 

There are models for database entities and ViewModels, organised in separate folders and namespaces. Views only use ViewModels with properties relevant to the view, as opposed to directly using models that represent database entries, in order to prevent accidents from direct access and ensure database integrity.

There are separate controllers for handling different user/role functionality, with most restricting access only to one specific role (e.g. lecturer only for the lecturer controller). Some controllers allow multiple roles, for example the controller for managing lecturer modules allows admins and program coordinators.

== Database Structure
The database contains tables for ASP.NET Core Identity users and roles, modules, lecturer modules, contract claims, uploaded files, and contract claim documents. 

The users table stores user information such as email, hashed password, etc. Lecturers, program coordinators, and academic managers are all stored as users, with their respective roles used to distinguish them. 

The modules table stores module name and code. Lecturers can teach multiple modules, and modules may be taught by multiple lecturers, with the lecturer modules table to keep track of who teaches what. Contract claims reference the lecturer who created the claim, the module taught by the lecturer, hours worked, hourly rate, lecturer comment, the program coordinator and academic manager who reviews the claim along with their review decision and an optional comment, and finally the status of the claim. A contract claim may have multiple supporting documents, which are stored as uploaded files and linked to their respective claims by the contract claim documents table.

== UML Diagram
The UML diagram depicts the database entities and their relationships with each other. For the sake of simplicity, I have depicted the "User" entity/class being extended by "LecturerUser" and "ReviewerUser", which is further extended by "ProgramCoordinator" and "AcademicManager". In the actual project, they are all simply users with roles assigned to distinguish them. The functionality depicted in the diagram is not implemented by the models themselves, but rather controllers with access restricted by role, for example lecturer users have access to the lecturer controller/views and the create claim functionality.

=== Relationships
- Lecturer users may have zero to many lecturer modules, with each lecturer module referencing one lecturer and one module, with modules referenced by zero to many lecturer modules.
- Lecturers may have zero to many contract claims, with each contract claim referencing one lecturer.
- Modules may be referenced by zero to many contract claims, with each contract claim referencing one module.
- Program coordinators may review zero to many contract claims, with each contract claim referencing zero or one program coordinator based on whether one has reviewed it yet.
- Academic managers may review zero to many contract claims, with each contract claim referencing zero or one academic managers based on whether one has reviewed it yet.
- Each contract claim document refers to one contract claim and one uploaded file, with contract claims having zero to many contract claim documents, and uploaded files referenced by zero to many contract claim documents.

== GUI Layout
== Theme and Colour Scheme
I use the Flatly bootstrap theme, which features a flat and modern aesthetic with a neutral yet attractive colour-scheme @park_2013_bootswatch. I chose this theme because it looks good, it's easy on the eyes but isn't over the top or obnoxious. It helps to distinguish my web application from the default theme featured in every ASP.NET Core MVC web application.

== Navigation Bar
The GUI features a navigation bar at the top, allowing users to easily navigate to and access different parts of the web application. The items in the navigation bar are dynamic, changing based on the role of the current user. If the user is not logged in or does not have a role yet, it only displays links to the home and privacy pages. Otherwise, it will display links to the user's role specific claim dashboard view, and additional links to other views featuring role specific functionality (such as role management for admins, lecturer module management for admins/program coordinators).

== Lecturer 
=== Claims Dashboard
The lecturer claims dashboard features a prominent create claim button, and two tables displaying pending and completed claims. The claims shown include useful information such as payment amount, reviewer decisions, and claim status, with a details button for each claim that goes to the claim details view to show more information. This design is practical, allowing lecturers to easily view claims status and information, as well as create a new claim with a click of a button.
=== Create Claim
The create claim page lets a lecturer select which module the claim is for, input the hours worked, hourly rate, optional comment, upload multiple supporting documents, and submit the claim or return to the dashboard with a click of a button. There is also a helpful preview of the total payment amount calculated based on hours worked and hourly rate. The design is intuitive and user-friendly.
=== Claim Details
The claim details page shows all the information that a lecturer is allowed to see, including: module name, hours worked, hourly rate, payment amount, lecturer comment, program coordinator decision and comment, academic manager decision and comment, claim status, and any uploaded supporting documents with options to download them. The design is simple and straightforward. 

== Academic Manager
=== Claims Dashboard
The academic manager claims dashboard features tables displaying pending claims that may be reviewed, claims pending further confirmation, and completed claims. Each claim has a button that directs to details about the claim and enables the claim to be reviewed. The design provides a clear overview of existing claims.
=== Claim details/review
The claim details page for academic managers shows more information than the lecturer's version, with additional sections for: lecturer name, program coordinator name, and academic manager name. Additionally, if the claim has not yet been reviewed by an academic manager, there is a section to review the claim with an optional comment and simple accept/reject buttons. The design is clear and functional, displaying relevant information and allowing review.

== Program Coordinator
=== Claims Dashboard
The claims dashboard for program coordinators is very similar to that of academic managers, with the difference being that pending claims are claims previously reviewed by an program coordinator not an academic manager. 
=== Claim details/review
The claim details/review for program coordinators is also very similar to that of academic managers, with the difference being that the review is done by a program coordinator.
=== Manage Lecturer Modules
This view is only accessible by admins or program coordinators. It displays lecturers and the modules taught by them, as well as a list of all modules, with options to add/remove modules and edit modules taught by specific lecturers.

== Admin
=== Manage User Roles
Displays all application users and their respective roles with options to change specific user roles, as well as a list of all roles with options to add new roles or delete existing roles. The purpose of this page is to allow admins/developers to modify user roles via the front-end instead of the back-end.

== Project Plan
The project plan consists of epics for each part of this assignment, with tasks outlining things to do for each part, with subtasks describing things to do more specifically. The plan includes dependencies as well as start and end times for epic and tasks.

#pagebreak()
= UML Class Diagram
#figure(
  image("images/Contract Monthly Claim System.drawio.png"),
  caption: [The UML Class Diagram.]
)


#pagebreak()
= Project Plan
The project plan was created using Jira. The plan includes epics for parts 1-3 of the POE, as well as tasks with subtasks. To view all the tasks with their subtasks you will have to view the project on Jira. You may view the project plan via #link("https://st10286905.atlassian.net/jira/software/projects/PROG/list?atlOrigin=eyJpIjoiZDRhMzFkOTA4NDJlNDJlNDhlZGRhNWI3MWQ4NTE5MWQiLCJwIjoiaiJ9")[this Jira link].

#figure(
  image("images/Project-Plan_Timeline.png"),
  caption: [The Project Plan Timeline.]
)


#pagebreak()
= GUI/UI Showcase

== Lecturer Views
#figure(
  image("images/screenshots/CMCS/CMCS - Lecturer Claims Dashboard.png"),
  caption: [Lecturer Claims Dashboard]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Lecturer Create Claim.png"),
  caption: [Lecturer Create Claim]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Lecturer Claim Details.png"),
  caption: [Lecturer Claim Details]
)

== Academic Manager Views
#figure(
  image("images/screenshots/CMCS/CMCS - Academic Manager Claims Dashboard.png"),
  caption: [Academic Manager Claims Dashboard]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Academic Manager Review Claim 1.png"),
  caption: [Academic Manager Review Claim 1]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Academic Manager Review Claim 2.png"),
  caption: [Academic Manager Review Claim 2]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Academic Manager Claim Details.png"),
  caption: [Academic Manager Claim Details]
)

== Program Coordinator Views
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Claims Dashboard.png"),
  caption: [Program Coordinator Claims Dashboard]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Review Claim 1.png"),
  caption: [Program Coordinator Review Claim 1]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Review Claim 2.png"),
  caption: [Program Coordinator Review Claim 2]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Claim Details 1.png"),
  caption: [Program Coordinator Claim Details 1]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Claim Details 2.png"),
  caption: [Program Coordinator Claim Details 2]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Manage Lecturer Modules.png"),
  caption: [Program Coordinator Manage Lecturer Modules]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Program Coordinator Assign Lecturer Modules.png"),
  caption: [Program Coordinator Assign Lecturer Modules]
)

== Admin Views
#figure(
  image("images/screenshots/CMCS/CMCS - Admin Manage Roles.png"),
  caption: [Admin Manage Roles]
)
#figure(
  image("images/screenshots/CMCS/CMCS - Admin Assign User Roles.png"),
  caption: [Admin Assign User Roles]
)

#pagebreak()
= AI Usage and Disclaimer
While working on this project I made use of ChatGPT to assist me @openai_2025_chatgpt. Note that in the project source code I reference AI usage with comments where it is used and add links to ChatGPT chats. Information regarding why and where it was used may be found listed below in no particular order.

- `LecturerController.cs` 
  - ChatGPT taught me how to implement file uploads for the project. The file upload/download code found throughout the project was influenced by ChatGPT.
  - Link: https://chatgpt.com/share/68cac0b4-34b8-800b-b47c-a65ef55ad8e5
  - Screenshot: #image("images/screenshots/ChatGPT/LecturerController_file-upload-download.png")
#pagebreak()
- `LecturerModuleManagerController.cs`
  - I asked ChatGPT to create a controller for managing lecturer modules to save time.
  - Link: https://chatgpt.com/share/68c1723e-d2a8-800b-93c7-41da82b21c0e
  - Screenshot: #image("images/screenshots/ChatGPT/LecturerModuleManager.png")
#pagebreak()
- `UserRoleManagerController.cs`
  - I asked ChatGPT to create a controller for managing user roles to save time.
  - Link: https://chatgpt.com/share/68c17e75-6410-800b-922a-8487a7e06720
  - Screenshot: #image("images/screenshots/ChatGPT/UserRoleManagerController.png")
#pagebreak()
- `DatabaseSeeder.cs`
  - I asked ChatGPT to help me troubleshoot some issues I was having with the database seeder.
  - Link: https://chatgpt.com/share/68c04c01-77a4-800b-ac30-db12e569f8af
  - Screenshot: #image("images/screenshots/ChatGPT/DatabaseSeeder.png")
#pagebreak()
- `EnumExtensions.cs`
  - I asked ChatGPT about some utility functions I made and learned about C\# extensions.
  - Link: https://chatgpt.com/share/68c7f73a-4588-800b-a812-e5ef790cd5b1
  - Screenshot: #image("images/screenshots/ChatGPT/EnumExtensions.png")
#pagebreak()
- Most `.cshtml` views
  - I asked ChatGPT to help improve the look and feel of most of my views. I made follow-up prompts to change things if they didn't look right for some of them. Note that the links in the source code are included in each `view.cshtml` where ChatGPT was used, with the links being different because they were generated throughout different stages of the conversation. 
  - Link: https://chatgpt.com/share/68ca99b5-dc0c-800b-b554-c315e49df063
  - Screenshot: #image("images/screenshots/ChatGPT/AllViews.png")

#pagebreak(weak: true)
#bibliography("refs.bib", title: [References], style: "iie.csl")