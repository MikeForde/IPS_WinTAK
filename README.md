# IPS WinTAK Plugin

## Overview

The IPS WinTAK Plugin integrates with WinTAK to provide several key functions:

- **Map Object Inspection:** Tap on a map object to retrieve and process its CoT (Cursor-on-Target) message.
- **IPS Data Extraction:** Decode Base64 + GZip compressed IPS JSON data embedded within the CoT message.
- **Dynamic API Retrieval:** Extract a packageID from the IPS JSON and use it to perform an API call (`/tak/browser/:id`) that returns an HTML page.
- **WebView2 Integration:** Display the returned HTML page in a modern, interactive WebView2 window.
- **Two-Way Communication (Optional):** Support bi–directional messaging between the embedded webpage and the WinTAK plugin via WebView2.

## Features

- **User Interaction:** Activate inspection mode by clicking the "Map Item (IPS) Inspect" button. The plugin prompts the user to select a map object.
- **Data Processing:** Automatically extracts the ipsData XML element from the CoT message, decompresses the Base64 gzipped content to yield JSON, and then parses that JSON to obtain a packageID.
- **Dynamic API Call:** Uses the packageID to construct a URL (e.g., `http://localhost:3000/tak/browser/:packageID`) for an HTTP GET request that returns an HTML page.
- **Modern WebView:** Displays the HTML page inside a dedicated window using WebView2. A separate WebRecordView button also supports this functionality.
- **Web Messaging:** (Optional) WebView2 messaging allows the webpage to send data back to WinTAK without using the Internet.

## Installation

### Prerequisites

- **WinTAK:** Ensure you have WinTAK (version 5.3.0.161 or later) installed.
- **.NET Framework:** Target framework should be at least .NET Framework 4.8.1.
- **NuGet Packages:**
  - `Microsoft.Web.WebView2` (version 1.0.3179.45)
  - `Microsoft.Toolkit.Uwp.Notifications`
  - `Newtonsoft.Json`
  - Plus other WinTAK–related dependencies

### Bundling WebView2 Fixed Runtime

1. **Download the Fixed Version:**
   - Download the WebView2 fixed version runtime as a CAB file from the [official site](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section).

2. **Extract the CAB File:**
   - You can extract it using the `expand` command or with a tool like 7-Zip:
     ```cmd
     expand -F:* "C:\Path\To\WebView2FixedRuntime.cab" "C:\Path\To\FixedRuntimeFolder"
     ```

3. **Include the Files in Your Project:**
   - Create a folder (for example, `WebView2Fixed`) in your plugin project.
   - Copy the required files (such as `Microsoft.Web.WebView2.Wpf.dll`, `Microsoft.Web.WebView2.Core.dll`, and `WebView2Loader.dll`) from the extracted folder.
   - In Visual Studio, set these files’ **Build Action** to **Content** and **Copy to Output Directory** to **Copy if Newer**.

4. **Update Initialization Code:**
   - In your WebViewWindow code, pass the fixed runtime folder path to `CoreWebView2Environment.CreateAsync`:
     ```csharp
     string fixedVersionFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Fixed");
     CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(fixedVersionFolder, userDataFolder);
     ```

## Usage

### Map Object Inspection & IPS Data Retrieval

- **Inspect IPS Data:**
  - Click the "Map Item (IPS) Inspect" button.
  - A prompt instructs you to tap on a map object.
  - The plugin retrieves the CoT message from the selected object, locates the `<ipsData>` element, decodes the Base64 gzipped content to JSON, and parses it.
  
- **Retrieve and Display IPS Record:**
  - The IPS JSON is parsed to extract a `packageID`.
  - An HTTP GET call is made to the API URL (`/tak/browser/:packageID`).
  - The returned HTML is displayed in a dedicated WebView window.

### WebView2 Integration & Messaging

- **Displaying HTML in WebView2:**
  - The WebViewWindow supports both URL navigation and loading HTML via `NavigateToString`.
  - You can also send messages from the webpage to WinTAK using `window.chrome.webview.postMessage()`.

- **Receiving Messages in WinTAK:**
  - The plugin subscribes to the `WebMessageReceived` event and processes incoming messages from the webpage.

### Example Workflow for Web Record Viewer

1. **User clicks "Web Record View" button.**
2. **User is prompted to tap on a map object.**
3. **CoT data is retrieved from the object; ipsData is decoded to JSON.**
4. **The packageID from the JSON is used to build the API URL.**
5. **HTML content is fetched via an HTTP GET call and displayed in a WebView window.**
6. **(Optional) The webpage can send messages back to WinTAK.**

## Troubleshooting

- **WebView2 Loader Issues:**
  - Ensure that all WebView2 fixed runtime files are bundled in your plugin folder and accessible.
  - If you see errors such as "Class not registered" or "Access Denied," verify your file paths and permissions. Consider using a custom user data folder.

- **HTTP 404 or API Issues:**
  - Verify that the API endpoint is correct.
  - Use logging to compare the endpoint with what you see working in your API testing tools (e.g., Insomnia).

- **JSON Parsing Errors:**
  - Ensure the `<ipsData>` element exists in the CoT message.
  - Verify that the Base64 encoded data follows the expected GZip format.

- **Web Messaging:**
  - Check that your webpage’s JavaScript is using `window.chrome.webview.postMessage()` and that your host subscribes to `WebMessageReceived`.

## Dependencies

- **Microsoft.Web.WebView2 (Fixed Version Distribution)**  
- **Microsoft.Toolkit.Uwp.Notifications**
- **Newtonsoft.Json**
- **WinTAK-Dependencies & Elevation Tools**  
- **Other WinTAK libraries**

## Redistribution and Licensing

When bundling the fixed version of the WebView2 runtime, ensure compliance with Microsoft’s redistribution guidelines. Review the licensing terms provided by Microsoft and confirm that your deployment of the runtime meets those terms.

---

*This README was generated to serve as documentation for the IPS WinTAK Plugin and can be bundled with your release package.*
