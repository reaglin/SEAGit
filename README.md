# SEAGit (Super-Easy and Accessible Git)

Welcome to SEAGit! This tool is designed to completely remove the complexity of version control. No branching, no staging, no command-line jargon—just a simple way to take a folder on your computer and publish it directly to GitHub.

Whether you are saving backups of your code or publishing a simple HTML website, SEAGit makes it a one-click process.

### 📥 Download and Install
**[Download the latest SEAGit Installer for Windows](https://github.com/reaglin/SEAGit/releases/latest)**

*(The installer will automatically check for and download the required .NET 8 Runtime if your system needs it.)*

---

## 🚀 How to Use SEAGit (Step-by-Step)

### Step 1: Get the Basics

Before using SEAGit, you need two things:

1. **A GitHub Account:** Go to [GitHub.com](https://github.com/) and create a free account. Your files need a home in the cloud!

2. **Nothing else.** Git comes built into SEAGit; you do not need to install it.

### Step 2: Create a Home for Your Files on GitHub

1. Log into your GitHub account.

2. Click the **"+"** icon in the top right corner and select **"New repository"**.

3. Give your repository a name (e.g., `my-cool-website`).

4. Keep it Public or Private, but **do not** check the boxes to add a README, .gitignore, or license. You want a completely *empty* repository.

5. Click **"Create repository"** and copy the URL it gives you (it will look like `https://github.com/your-username/your-repo-name.git`).

### Step 3: Work on Your Computer

1. Create a normal folder anywhere on your computer.

2. Put your stuff in it! Write your code, build your HTML website, or add your project files.

### Step 4: Publish with SEAGit

1. Open the SEAGit application.

2. Click **"+ Add Local Folder…"** and select the folder you created in Step 3. The first time, SEAGit asks before it prepares the folder for publishing. (It will not publish a whole drive or your whole user folder.)

3. Select your folder from the list and click **"Publish Selected"**.

4. **The Link:** If this is your first time publishing this folder, SEAGit asks for your GitHub repository address. Paste the link you copied in Step 2 (it must start with `https://github.com/`). SEAGit then tells you how many files will be published, so you can check nothing private is in the folder. Files over 100 MB cannot go to GitHub.

5. **The Login:** A secure Windows pop-up will appear asking you to "Sign in with your browser." Click it, log into GitHub, and authorize. You usually only have to do this once!

That's it! SEAGit says "Published N files to GitHub." and your files are safely on GitHub. Next time you make changes to your files, just open SEAGit and click "Publish Selected" — no address or sign-in needed. If something goes wrong (no internet, sign-in not finished, a GitHub repository that is not empty), SEAGit says so in plain words and what to do; whatever was not sent is sent the next time you press Publish Selected. Use **Change GitHub Address…** if the address was wrong, **Remove from List** to take a folder off the list (your files are not deleted), and **Help ▸ Help for this screen** (F1) for help.

---

### 🌐 Bonus: Turn it into a Live Website!

If you pushed HTML files (like an `index.html`):

1. Go to your repository on GitHub.

2. Click **"Settings"** (the gear icon near the top).

3. On the left sidebar, click **"Pages"**.

4. Under "Build and deployment", look at the "Branch" section. Change the dropdown from "None" to the branch SEAGit published to — **"main"** for folders SEAGit prepared (an older folder may use **"master"**) — and click **Save**.

5. Give it a minute or two, and your website will be live on the internet!

---
*Conceptualized and built by Ron Eaglin, with code architecture produced in conjunction with Google Gemini.*
