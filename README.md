# My Little C# Web Server

Hi! This is a simple web server I whipped up with C#. It's all about serving basic website files (HTML, CSS, JS) from a local `webroot` folder directly to a browser.

## What it Does:

*   Serves your static web files (`.html`, `.css`, `.js`) when a browser asks (using GET).
*   Listens on port 8080 for connections.
*   Can chat with a few browsers at once.
*   Sends simple error pages if a file is missing (404), the request method is wrong (405), or if access isn't allowed (403).
*   Keeps files secure by only looking inside the `webroot` folder.

## Key Parts:

*   `Program.cs`: The main code for the server.
*   `webroot/`: Put your website files here!

## Quick Start:

1.  **Build:** Use the .NET SDK to build the project (`dotnet build`).
2.  **Run:** Execute the program from its output folder (e.g., `dotnet WebServer.dll`).
3.  **View:** Open `http://localhost:8080/` in your browser.
