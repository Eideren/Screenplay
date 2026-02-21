Requires https://github.com/Eideren/YNode

# Screenplay

An all encompassing tool for games' narrative. Covering interactions, events, dialogs, branching story, cutscenes,  AI behavior, scene changes, saving and loading of progress, and more. Within the same canvas to design complex sequences using all of those systems simultaneously.

<img width="1126" height="655" alt="Image" src="https://github.com/user-attachments/assets/e04853e5-2ec7-4732-87d3-ecc436b79c77" />



## Nodes

### Branch

Once this node is reached, it will run one of the two connection depending on the condition or prerequisite provided.

### Simultaneous

Most nodes in a screenplay run sequentially; once a node is done, the next one runs.
In some cases, you may want to run two or more nodes at the same time, this is what Simultaneous is for, any nodes it is connected with are run at the same time.

### Rejoin

Join is used in conjunction with Simultaneous, paths running from Simultaneous run in parallel as fast as they can. You may need paths to wait for a specific node to be done before continuing, for example; when an NPC is waiting for the player to answer, you may play the animation alongside the dialog and place a join at the end of those two paths to continue on from there only after the player passed the dialog.

A pattern you may find yourself to want is one where you have a subset of paths that have to wait for each other, but another set that doesn't or don't have to wait at the same point. Screenplay does not allow partial rejoin, meaning that if three paths come out of a Simultaneous, those three paths must all reach the same Rejoin if any of them does. But there is a fairly straightforward solution:
Let's say you want three branches, two to move a NPC while having a dialog, and another that has another NPC walking in the background doing all sorts of stuff disregarding the other branch completely.
You'll have to make two simultaneous, the first one splits in two, one part running the second NPC, and another that leads directly into another Simultaneous which will run the dialog and first NPC animation splitting and rejoining.

### INodeWithSceneGizmos
