#import "@preview/grape-suite:3.1.0": exercise
#import exercise: project, task, subtask

#show: project.with(
    title: "PROG6212 POE Part 1",
    author: "Sky Martin",
    show-outline: true
)
#set text(lang: "en", region: "GB")

#pagebreak()
= Video Showcase Of The WebApp
Here is a link to the video: https://www.youtube.com/watch?v=TODOMAKEVIDEO


= Feedback and Changes From Part 1 To Part 2
== Feedback
*GUI/UI*: "Clean UI. Nice use of colours to improve readability. Gone beyond and included a module manager. Handled scalability with a user role manager as well. Excellent job - well done Sky." 

*Project Plan*: "Could divided into more tasks for easier delegation"

=== Addressing Feedback
Regarding the *GUI/UI*: Thank you very much. I am very glad my hard work and effort paid off!

Regarding the *Project Plan*: I did have more tasks, but they were subtasks and unfortunately were not visible in the Timeline view on Jira. They were accessible via the Jira project link, but unfortunately I have just now realised that the link itself only works if you're logged into my Jira account. Below you will find screenshots of the original Timeline view and the List view showing all tasks/subtasks for the Jira project.
#image("images/Project-Plan_Timeline.png")
#image("images/screenshots/Jira/Jira-List (1).png")
#image("images/screenshots/Jira/Jira-List (2).png")
#image("images/screenshots/Jira/Jira-List (3).png")
#image("images/screenshots/Jira/Jira-List (4).png")
#image("images/screenshots/Jira/Jira-List (5).png")

== Changes From Part 1 To Part 2
- *Refactor to Use Services*: To make implementing unit tests more straight-forward, I decided to refactor and move the main functionality into relevant service classes.
- *Encrypt File Uploads*: Uploaded files are now encrypted for storage, and decrypted for downloads.
- *Improve File Upload Validation and Error Display*: Uploaded files are now properly validated to ensure they are a valid file type, within the size limit, and error messages are shown clearly to the user where applicable.
- *Implement Unit Tests For Main Functionality*: Unit tests were created for the main functionality of the app, including every service and service method, in addition to every controller and controller method.


#pagebreak()
= AI Usage and Disclaimer
While working on this project I made use of ChatGPT to assist me @openai_2025_chatgpt. Note that in the project source code I reference AI usage with comments where it is used and add links to ChatGPT chats. Information regarding why and where it was used may be found listed below in no particular order.

- Refactor project to use services (for better unit tests)
  - ChatGPT explained that the project will be easier to unit test if I refactor it to make use of service classes for the main functionality.
  - Link: https://chatgpt.com/share/68f3b4ed-3c8c-800b-a26a-d00d7f3b3409
  - Screenshot: #image("images/screenshots/ChatGPT/ServiceRefactor.png")
#pagebreak()
- Encryption/decryption for file uploads/downloads
  - ChatGPT showed me how to implement file encryption for encrypting files for storage, and decrypting them again when downloaded.
  - Link: https://chatgpt.com/share/68f3f2c2-0354-800b-bd9a-666184acbc34
  - Screenshot: #image("images/screenshots/ChatGPT/FileEncryptionDecryption.png")
#pagebreak()
- File upload validation and error display
  - ChatGPT showed how to go about validating file uploads, and how to display more descriptive errors for invalid files.
  - Link: https://chatgpt.com/share/68f4b905-8f30-800b-9f73-1bb2052cdbaa
  - Screenshot: #image("images/screenshots/ChatGPT/FileValidationErrors.png")
#pagebreak()
- Unit tests
  - ChatGPT helped me to create the unit tests for the application. It was a collaborative effort, with a lot of back and forth.
  - Link: https://chatgpt.com/share/68f5452c-2788-800b-bbbc-175029690cfd
  - Screenshot: #image("images/screenshots/ChatGPT/UnitTests.png")
/*
#pagebreak()
- Where
  - What
  - Link: 
  - Screenshot: 
*/
#pagebreak()
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