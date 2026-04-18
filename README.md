# SEAGit (Super-Easy and Accessible Git)

Welcome to SEAGit! This tool is designed to completely remove the complexity of version control. No branching, no staging, no command-line jargon—just a simple way to take a folder on your computer and publish it directly to GitHub.

Whether you are saving backups of your code or publishing a simple HTML website, SEAGit makes it a one-click process.

## 🚀 How to Use SEAGit (Step-by-Step)

### Step 1: Get the Basics

Before using SEAGit, you need two things:

1. **A GitHub Account:** Go to [GitHub.com](https://github.com/) and create a free account. Your files need a home in the cloud!

2. **Git for Windows:** Download and install [Git for Windows](https://gitforwindows.org/). You can just click "Next" through the default installer options. SEAGit uses this engine quietly in the background.

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

2. Click **"+ Add Local Folder"** and select the folder you created in Step 3. SEAGit will automatically prepare it for you.

3. Select your folder from the list and click **"Publish Selected"**.

4. **The Link:** If this is your first time publishing this folder, SEAGit will ask for your GitHub URL. Paste the link you copied in Step 2.

5. **The Login:** A secure Windows pop-up will appear asking you to "Sign in with your browser." Click it, log into GitHub, and authorize. You usually only have to do this once!

That's it! Your status log will say "Successfully pushed," and your files are safely on GitHub. Next time you make changes to your files, just open SEAGit and click "Publish"—no URLs or logins required.

---

### 🌐 Bonus: Turn it into a Live Website!

If you pushed HTML files (like an `index.html`):

1. Go to your repository on GitHub.

2. Click **"Settings"** (the gear icon near the top).

3. On the left sidebar, click **"Pages"**.

4. Under "Build and deployment", look at the "Branch" section. Change the dropdown from "None" to **"main"** and click **Save**.

5. Give it a minute or two, and your website will be live on the internet!

---
*Conceptualized and built by you, with code architecture produced in conjunction with Google Gemini.*
