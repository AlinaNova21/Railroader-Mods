2024/06/09
- selected node chevron is rendered with magenta color
- keyboard support for node editing
    - numpad keys 4,5,6,7,8,9 correspons to ui move / rotate buttons
    - numpad enter toggles between move / rotate operation
    - numpad + and numpad - changes scale
    - numpad * and numpad / changes scale delta
    - numpad 0 resets scale to 0
- refactored code that is editing node from UIMoveTool to NodeManager class
- refactored UIMoveTool BuildWindow method
- refactored StateTracker classes
    - glost classes store info about deleted node/segment for undo/redo magic to work
    - it will also not store those values in ctor, because they may change before Apply/Revert is called

- added 'Split' action to node editor
    - this will remove selected node and replace it with multiple nodes roughly at the same location with track segments connections from old node 'other' nodes
- added 'Remove' action to node editor
    - this will remove node and its segments
    - alt mode for simple rail: removing 'noce B' from (node A --- node B --- node C) will result in (node A --- node C)
- added 'Copy rotation' and 'Paste rotation' actions to node editor
    - first one will store node rotation and second one will create ChangeTrackNode action that will apply that rotation  
- added 'Copy elevation' and 'Paste elevation' actions to node editor

issues:
- when I quit to main menu with editor open and start new game editor is still active and also start throwing exception, because some game objects where destryoed
  - tryied to register OnMapDidUnload event but didnt removed editor correctly ...
- when I close UIMoveTool im unable to open it again
  - i 'fixed' that by not removing event handler in OnDeactivating
- game sometimes do not redraw rails correctly ('Rebuild Track' usually fixes that)
  - i think that rebuild is not calling something / its timing issue
- any reason behind `end_of_line = lf` in .editorconfig?
  - git can handle converting those quite nicely
  - on windows is really annoing to deal with 'Incostintent Line Endings' dialog every time i open or edit file