# PaintProject_Window-Development##### Lecturer Instruction: **Mr Trần Duy Quang**
###### _Web I used to write this markdown file: https://dillinger.io_ (You should use this website to ensure the correct formatting for display. )
# ==_PROJECT 2 - PAINT PROJECT_==
#
#
#### ++1. Members of the group++
1. 20127533 - Lê Đăng Khoa
2. 20127475 - Nguyễn Trần Đại Dương
3. 20127596 - Nguyễn Như Phước
4. 20127599 - Lê Quân
****
#### ++2. Important points to remember when running the program++
+  **++_Paint Project_++** uses many Ribbon UI and many components from the [`Telerik`](https://www.telerik.com/products/wpf/overview.aspx) library. Because this library is a paid service, our group downloaded it illegally from a third-party source for educational purpose. Therefore, if you want to compile the program directly from the code, you can download the library via the following link: [`Link download from my Google drive`](https://drive.google.com/drive/folders/1FeuZlBZSiXBlq1l5fDT_VZplDZ0NMvPP?usp=sharing) 
+  There are 4 abilities class library imported to Paint Project and we set their complie files to PainProject folder. So if you complie the program, make sure you build Paint Project first (Because 4 ablilities need IShape Contract in main project)
 [![N|Solid](https://i.imgur.com/1Y9hkjh.png)](https://i.imgur.com/1Y9hkjh.png)
+ There are 2 ways to run application
-- Run executable file `a_PaintProject.exe` from `Release Folder` (prefix `a_` in filename make it appear at top, easier for teachers to see)
 [![N|Solid](https://i.imgur.com/gOPFszw.png)](https://i.imgur.com/gOPFszw.png)
-- Install `Setup` file from `Setup folder`. When install app, a shortcut to run app will appear at your Desktop
==+ If you run the app using method 2, make sure the app Run as administrator (Group have already set requestedExecutionLevel in manifest is `requireAdministrator`)==

****
#### ++3. Features our group has completed++
##### _Basic features_
1. **Dynamically load all graphic objects that can be drawn from external DLL files**: There are 4 types of drawing compiled into DLL abilities files. The main program will load these DLLs dinamically to enable the shapes installed to be drawn.

2. **User can choose which object to draw**: Using the ribbon UI to select shapes that the program supports.
[![N|Solid](https://i.imgur.com/Knbp9gp.png)](https://i.imgur.com/Knbp9gp.png)

3. **User can see the preview of the object they want to draw**:  We have improved upon teacher's example, where the shapes can now be drawn in `4 directions` (according to StartPoint), and `only the current shape's preview is cleared` instead of redrawing all existing shapes
[![N|Solid](https://i.imgur.com/k4odRJt.gif)](https://i.imgur.com/k4odRJt.gif)

4. **Finish drawing**: The user can finish the drawing preview by releasing the mouse and their change becomes permanent with previous drawn objects

5. **Save as binary format**: User can save drawn objects to binary file (.bin) and reload the file to continue drawing. In this feature, we have further improved by adding the Recent File function, which shows recent saved files so that users can re-open them more quickly.
 [![N|Solid](https://i.imgur.com/FosI92O.png)](https://i.imgur.com/FosI92O.png)

6. **Export to image file**: Users can export their drawn images into image files. Supporting 3 types of extension is `.png`, `.ipg` and `.bmp`

##### _Advanced Features:_
1. **Change the color, pen width, stroke type**: User can choose colors, width and stroke types for next drawing shape, and change for drawn shape
 [![N|Solid](https://i.imgur.com/UHOgXHO.png)](https://i.imgur.com/UHOgXHO.png)
2. **Text Ablility**: In addition to the 3 basic graphic objects, the application also has Text Ability, which allows user to type text on the drawing space. However, this capability still encounters some errors when saved in binary format, or cannot apply the erase function
3. **Adding images to canvas**: Images can be imported to canvas as well as user can move, rotate and delete it

4. **Reduce flickering**: We store last preview of drawing shape, only last preview shape was redraw, not all shape on canvas. So flickering has been significantly reduced.

5. **Select single element**: User can select element (exclude TextAbility) for moving, rotating, change color, stroke type, thickness. Images inserted can be selected as a Shape
 [![N|Solid](https://i.imgur.com/yQFLJNG.gif)](https://i.imgur.com/yQFLJNG.gif)

6. **Zooming**: Zoom function in Paint allows you to magnify or reduce the view of your canvas.


7. **Eraser mode**: Allowing user to remove whole shape by holding down the left mouse button and moving the mouse to the shape you want to erase (Exclude TextAbility)

8. **Undo - Redo**: The Undo function allows you to reverse your last action. The Redo function, on the other hand, allows you to redo the action that you have just undone (Available for drawing shapes only)

9. **Holding shift to draw special shape**: When shape selected is Ellipse, when user hold shift button, the drawn shape is Circle, if user select Rectangle type, drawn shape is Square.

10. **Fill color by boundaries**: The feature allows users to fill a selected area with a color of their choice until it touches a boundary using Scan line flood fill Algorithm. BUT, feature still has many limitations, such as slow coloring speed and incorrect coloring when using the zoom feature (because the color on the pixel being filled will be determined incorrectly)

11. **Create a new drawing**: User can create new drawing in File -> New, if current canvas already have shape, application will show confirmation for user to save before create a new canvas 

#### ✨++Expected Grade: 
1. 20127533 - Lê Đăng Khoa : 10đ
2. 20127475 - Nguyễn Trần Đại Dương: 10đ
3. 20127596 - Nguyễn Như Phước: 10đ
4. 20127599 - Lê Quân: 10đ
#
#### ✨++Video demo Link (`Youtube`):++  https://youtu.be/oZzM2znIYtI
+ **_Please enable the subtitles for a detailed explanation_**
### `Thank you for reading our report `
## _END_
