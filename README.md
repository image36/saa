#Sir Archives-a-lot

## What does it do?

SAA can move, copy or delete files.  The affected files match a regular expression and optionally a last write date.

## How do I make it work?

Requires a single command line parameter: the path to an XML file that contains the directives 

Here is a [sample XML File](https://github.com/image36/saa/blob/master/sample.xml)

## Can I get an explanation of this XML file?

### directives 

All directives are contained in the root node "directives".

`<directives test="true" verbosity="2" log="c:\log\">`

* test:  This is a Boolean value (true or false).  When set to true no actions are taken, but messages are logged and displayed on what action would have happened.
* verbosity: 0-10.  How verbose should the log and console output be?  0 = quiet through 10 = talkative.
* log: The path to the log file.  This should specify the directory name, not a file name.  If the directory does not exist it will be created.

### command

All commands are separated into separate command nodes.

`<command id="command_1" name="move" path="c:\foo\" mask=".*" daysOld="0" targetPath="c:\bar" inverseMask="false"/>`

* id:  The ID of the command.  This is used in the log file and can help with debugging your directives.
* name: The name of the command to use.  Valid names are move, copy and delete.
* path:  The path to the source directory.  This is where the command will be executed.
* mask:  The [RegEx pattern](http://msdn.microsoft.com/en-us/library/1400241x(VS.85).aspx) used to match file names.
* daysOld:  This integer (whole number) is the number of days since the last write to the file.
* targetPath: This is where files will be moved or copied if the name of the command is "move" or "copy".
* inverseMask: This is a Boolean (true or false).  When set to false, files that match the mask will be effected by the action.  When set to true, files that *don't match* the mask will be effected by the action.


*Use SAA at your own risk.  If this causes damage to your data or hardware, sorry about that, you've been warned.*  (but feel free to report it)

The MIT license

 Copyright (c) 2013
 Permission is hereby granted,  free of charge, to any person 
 obtaining a copy of this software and associated documentation files 
 (the "Software"), to deal in the Software without restriction, 
 including without limitation the rights to use, copy, modify, merge, 
 publish, distribute, sublicense, and/or sell copies of the Software, 
 and to permit persons to whom the Software is furnished to do so, 
 subject to the following conditions:
 The above copyright notice and this permission notice shall be included 
 in all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
 EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
 OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARSING 
 FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE 
 OR OTHER DEALINGS IN THE SOFTWARE.
