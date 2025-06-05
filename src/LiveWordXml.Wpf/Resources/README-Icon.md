# Application Icon Setup

## Current Status

The application has been configured to use a vector-based icon in XAML format for the window icon, and the project file has been set up to use an ICO file for the executable icon.

## To Complete Icon Setup:

### Option 1: Convert the existing SVG to ICO

1. Use the SVG file at `app-icon.svg` in this folder
2. Go to any online SVG to ICO converter (such as cloudconvert.com, convertio.co, or convertico.com)
3. Upload the SVG file and convert it to ICO format
4. Download the ICO file and save it as `app-icon.ico` in this Resources folder

### Option 2: Create ICO directly

Create a 32x32 pixel ICO file with the following design:

- Blue circular background (#2563EB)
- White document shape with rounded corners
- Red XML angle brackets (< >)
- Gray content lines
- Green "live" indicator dot in top-right

### What's Already Done:

- ✅ Window icon is set using vector graphics (will show in title bar)
- ✅ Project file configured for ApplicationIcon
- ✅ XAML vector icon created and applied to MainWindow

### What's Missing:

- 🔲 ICO file at `Resources\app-icon.ico` (for exe icon and taskbar)

The vector icon will work for the window title bar, but you'll need the ICO file for the executable icon and taskbar display.
