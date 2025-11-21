# Authentication and Authorisation
## Authentication
- System uses a custom cookie based authentication system. 
- User passwords are hashed and salted to store in the database, avoiding plain-text. 
- When logging in, the entered password is hashed using the stored salt and compared to the stored hash to verify whether or not their password is correct.

## Authorisation
Users may have roles, with specific roles enabling access to role specific functionality. 


# Role Based Functionality
## **Admin** 
**Full access** to **management** functionality.

## HR
**Access** to **management** functionality, the **invoice system**, and **running auto review for** all **existing** auto review **rules** on behalf of all reviewer users that setup auto review rules.
- **Management Functionality**:
	- **Set user**'s **role**(s).
	- **Create/delete roles**.
	- **Create users with** an initial **role assigned**.
- **Invoice System**:
	- **Generate invoices** for approved claims, either one by one or all at once.
	- **Download** generated **invoices** for approved claims.
	- **Run auto review** function, **applying all** existing auto review **rules**, **on behalf of** reviewer users **based** on their **configured auto review rules** (i.e. if a reviewer set a rule that was applied, that reviewer will be credited for the review). This is **useful to automatically process applicable claims immediately**, so that invoices may be generated for automatically approved claims immediately **without relying on reviewer users** running auto review themselves.

## **Lecturer**: 
Has **access** to **lecturer claims dashboard**, **claim details** for their claims, and **create new claim function**.
- **Claims Dashboard**: Provides an **overview of** all **claims** belonging to the lecturer user, **sorted** by **review status**, with **options** to **view details** of specific claims and an option to **create** a **new claim**.
- **Claim Details**: Shows **additional details** about the claim, including:
	- **hours worked**, **hourly rate**, **comments** by lecturer or reviewers
	- **downloads** for **supporting documents** uploaded for the claim.
- **Create Claim**: 
	- **Select** which **module** the claim is for, which **automatically determines** the **hourly rate** based on what was **assigned by HR for** the specific **module and lecturer**. 
	- **Input** number of **hours worked**
	- **Estimated** total **payment** amount is **calculated** and **displayed in real-time**, **changing** based **on user input** (module selection, hours worked input).
	- Optionally input **comment**.
	- Optionally **upload** any number of **supporting documents**, provided each is `10mb` or less in size and one of any of the following file formats: `.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .md`.

## Reviewer (ProgramCoordinator/AcademicManager)
Has access to role specific **reviewer claims dashboard**, **claim details**, **review claim**, **configure auto review rules**, and **run auto review**.
- **Reviewer Claims Dashboard**: Provides an **overview of** all existing **claims**, **sorted** by **review status**, with **options** to: 
	- **view details** of specific claims and **review** them **if pending** review for the same reviewer type as the user (ProgramCoordinator or AcademicManager).
	- **run auto review** based on configured auto review rules.
	- **navigate to auto review rules configuration**.
- **Claim Details**: Shows **additional details** about the claim, including:
	- **hours worked**, **hourly rate**, **comments** by lecturer or reviewers
	- **downloads** for **supporting documents** uploaded for the claim.
- **Claim Review** (In **Claim Details** view): 
	- If the review decision is still pending for same reviewer type as the reviewer user (ProgramCoordinator or AcademicManager), then the reviewer user may review the claim by selecting one of the following:
		- `Verify` (if ProgramCoordinator)
		- `Approve` (if AcademicManager)
		- `Reject` 
	- Once the **claim** has been **reviewed by one review type**, the **claim**'s **status changes** from `PENDING` to `PENDING_CONFIRM`. 
	- As soon as the **claim** has been **reviewed by both review types**, the **claim**'s **status changes** to **either** `ACCEPTED` or `REJECTED` based on whether or not **both reviewer types accepted** (`Verify`/`Approve`) **or** either **rejected** the claim. 
- **Configure Auto Review Rules**:
	- **Manage** auto review **rules**, including functionality to **create**/**view**/**edit**/**delete** rules.
	- **Auto Review Rule**:
		- **Automatically applies** a claim **review** **if** the conditional **expression defined by** the **rule** evaluates to **true**.
		- **Applied claim reviews** correctly **credit** the **reviewer user** with the review, saving reviewer details in the claim appropriately based on user's reviewer type.
		- **Multiple rules may apply**, **overwriting** the **reviews of** previous **lower priority** auto reviews. Note that the **only claims evaluated** in the auto review process **are** claims **pending** review **at** the **beginning** of the **process**, so **reviews** may **only** be **overwritten** **by higher priority rules during** a **single** auto review **process**, which **once complete** may **not** be **overwritten again** if a review was applied.
	- **Properties** of **Auto Review Rules**
		- **ID**: identifies the rule
		- **Reviewer ID**: User ID of the reviewer user.
		- **Priority**: higher priority rules are applied later, thus overwriting lower priority rule decisions.
		- **Auto Decision**: either `PENDING`, `REJECTED`, `VERIFIED`, or `APPROVED` based on reviewer type. Applied only if rule condition evaluates to `true`.
		- **Comparison Var**: either `HOURS_WORKED`, `HOURLY_RATE`, or `PAYMENT_TOTAL`. Determines which claim variable to use when evaluating rule condition. The **value** of this variable is placed on the **left-hand side** of the **comparison operation** used to **evaluate** the **rule condition**.
		- **Comparison Op**: either `EQUAL`, `NOT_EQUAL`, `LESS_THAN`, `LESS_THAN_OR_EQUAL`, `GREATER_THAN`, or `GREATER_THAN_OR_EQUAL`. Determines which **comparison operation** is performed to **evaluate rule condition**.
		- **Comparison Value**: the **value** placed on the **right-hand side** of the **comparison operation** used to **evaluate** the **rule condition**.
		- **Auto Comment**: the **comment to leave** when rule condition is true and claim **review** is **applied**. **If left empty** a **comment** is **generated** to convey the **decision applied** and the **conditional expression** that **evaluated** to **true**, for example: `Automatically APPROVED claim because HOURS_WORKED = '1.00' is LESS_THAN_OR_EQUAL to '207.00'`


# Demonstration Notes

## User Registration
- Anyone may register using the registration form
	- No role is assigned by default
	- Without a role, only basic account management functionality is available:
		- Changing name, email, password, etc
		- Deleting account
	- HR must assign new users with relevant role(s)

## User Login
- Simple login form, which requires:
	- Username
	- Correct password
- If user exists: input password is hashed using user's stored password salt, then compared with stored password hash. If they match, the user is successfully successfully authenticated and logs in. 

## HR Manage Users & Roles 
- HR must evaluate newly registered users, and if appropriate: assign them relevant role(s) to allow them to access appropriate functionality
- HR may create new users with an initial role

## HR Manage Lecturers & Modules
- HR must manage (create/delete) all modules that may be taught by a lecturer.
- HR must manage lecturer information:
	- Assigning lecturer modules with specific hourly rates
	- Manage lecturer's personal information, including: contact number, address, and bank details. 

## Lecturer Claims Dashboard
- Create claim:
	- Lecturer can create a new claim for any module they teach
	- Specify which module the claim is for
	- Specify hours worked
	- View the hourly rate (read-only). Determined by selected module. Modules are assigned by HR, who also specify the lecturer + module hourly rate
	- Changing selected module or hours worked automatically calculates and updates the displayed estimated payment total
	- Optionally write a comment
	- Optionally upload multiple supporting documents, provided each file is `10mb` or smaller and one of the following file types: `.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .md`
		- Uploaded files are encrypted for storage, only decrypted byte by byte as they are downloaded.
- Claims Dashboard
	- Overview of claims with summarised info
	- Claims sorted based on review status
	- Buttons to view specific claim details
- Claim details
	- Displays all information about the claim relevant to the lecturer
	- Does not show who reviewed a claim for privacy reasons
	- Allows downloading any of the claim's uploaded supporting documents (if any).
		- Note: there is a sanity check which ensures the lecturer created the claim the file belongs to before allowing download to proceed.

## Reviewer Claims Dashboard
- While virtually identical, there are separate views for the different reviewer types (as required by the assignment instructions).
	- The difference between them is:
		- each may only review claims that have not been reviewed by the same reviewer type
		- pending reviews are based on reviews that are able to be reviewed by that reviewer type
		- the rest is duplicated code .. as required
- Claims Dashboard
	- Overview of claims with summarised info
	- Claims sorted based on review status
	- Buttons to view specific claim details
	- Button to Run Auto Review
	- Button to Configure Auto Review Rules
- Claim details
	- Displays all information about the claim relevant to the lecturer
	- Does show who reviewed a claim, unlike lecturer's view
	- Allows downloading any of the claim's uploaded supporting documents (if any)
	- Claim may be reviewed as long as it hasn't already been reviewed by matching reviewer type (ProgramCoordinator/AcademicManager).
- Claim review
	- Optional comment
	- Submitted by selecting review decision, which could be one of the following:
		- `Verify` (if ProgramCoordinator)
		- `Approve` (if AcademicManager)
		- `Reject` 
	- Review decision is immediately visible to other users, e.g. the lecturer who made the claim or other reviewer users.
- Run Auto Review
	- Automatically reviews all pending claims based on configured Auto Review Rules
	- Reviews only apply to claims when a matching rule's conditional expression evaluates to true.
	- In one Auto Review run, multiple rules may apply reviews, with higher priority rules overwriting lower priority rule reviews. Note that further runs won't overwrite completed reviews.
	- Automatically reviewed claims are correctly attributed to the reviewer user, saved under the claim's section for their reviewer type's (ProgramCoordinator/AcademicManager) info + decision + comment.
- Configure Auto Review Rules
	- Overview of existing Auto Review Rules as table entries, each with buttons to
		- Increase rule priority
		- Decrease rule priority
		- Edit rule
		- Delete rule
	- Run AutoReview button
		- Runs auto review, same as the button in the claims dashboard.
	- Add new rule form
		- Fields for all required Auto Review Rule properties
		- Add rule button
	- Edit rule
		- Allows completely editing the rule
		- Button button to cancel/go back
		- Button to save edits

## HR Invoices
- Button to Process All Invoices
	- Generates invoices for all approved claims that have not already had invoices generated.
- Button to Run Auto Review 
	- Runs on behalf of all reviewer users who have configured Auto Review Rules. 
	- Still correctly attributes claim reviews to relevant reviewer users. 
	- Useful to avoid waiting for a claim reviewer to review claims that may be reviewed automatically using their own claim review logic.
- Overview of existing approved claims
	- Shows basic claim summary info
	- Shows invoice file name if it exists
	- Generate invoice button for claims without invoices
	- Download button for claims with invoices
- Generated invoices are pdf documents
	- Encrypted and stored on disk, with file info saved in the database. 
	- When downloaded, they are decrypted as they are sent byte by byte. 
	- If encrypted file is deleted, a new invoice should be generated, saved, and served the next time there is a download request.

