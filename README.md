# Screenplay
A tool to create interactive fiction or add interactive story elements to a game.

## Table of Contents
1. [License](#license)
2. [Installation](#installation)
3. [Usage](#usage)
4. [Syntax](#syntax)
    1. [``// comments``](#``// xyz``)
    2. [``{command}``](#``{my command}``)
    3. [``= Passage =``](#``= My Passage =``)
    4. [``-> Go To``](#``-> My Passage``)
    5. [``<- Jump Back``](#``<-``)
    6. [``> Choice``](#``>``)
    7. [``< Close choice``](#``<``)
    8. [Incrementation](#incrementation)
    9. [Tags](#tags)
5. [Example](#example)

## License

This project is provided under the MIT license, see [LICENSE](LICENSE)

This package contains a heavily modified version of the MIT licensed [UnitySerializedReferenceUI](https://github.com/TextusGames/UnitySerializedReferenceUI)

Some icons provided by [game-icons.net](https://game-icons.net) under [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)

## Installation

- Press the `+` sign inside of the Unity Package Manager window
- Click on `Add package from git URL`
- Paste `https://github.com/Eideren/Screenplay.git` in the text field.
- Press Add

You may have to add a reference to `Unity.TextMeshPro` in `Screenplay/Screenplay.asmdef` through the
inspector if you have compilation issues.

## Usage

Here is a video which goes over how to use this system https://youtu.be/YnPIp8d5Cmo - see also the [example](#example) below.

### Syntax

We'll go into more details in this section about how the syntax work,
but there is an help button in the `Scenario` inspector which contains a cheat-sheet for these.

#### ``// xyz``
This is a comment line, it is ignored by default when the system outputs the scenario on screen.

You can leave yourself notes about what you're writing through these.

#### ``{my command}``
This can be placed anywhere on a line, it will execute the command of the same
name you set up through unity's inspector, see [Usage](#usage)

Setting that command to a `Show On Condition` in the inspector will hide the line or
choice it is on based on variables, game state and such.

#### ``= My Passage =``
Marks the start of a passage, a passage is a point of the script where
you can jump to using the ``-> My Passage`` command.

Execution will not continue into a new passage if you didn't explicitly jump
to it with that command, it will stop instead.

You can add as many `=` on either side as you need, so ``==== My Passage =`` is also valid.

Your passage name can contain any special characters as long as you don't split it across lines or
use `=`, so `=== A P8$$8g&-N8m& C0mpr1$1ng un&xp&c7&d+charac7&r$ ===` would be valid.

#### ``-> My Passage``
A Go to, we will continue reading from the first line under ``= My Passage =``.

#### ``<-``
Will exit this passage and return to the point where we jumped from, or close the scenario if this is the first passage.

If you want to make sure that the scenario closes, you can write `-> End` instead and add an empty `== End ==`.

#### ``>``
Marks choices, this line and all subsequent ``>`` line on the same incrementation level will be
presented together when reading.

If you increment the lines below that choice, those lines will get to play only when that choice was selected.
```
> Slay the dragon
    You remove your left shoe, hop towards the dragon and start whacking at his hind leg ...
> You can't, today is the National Mac and Cheese Day
```

#### ``<``
Marks the end of a series of choices, not required unless you are
doing two different series of choices without text to split them apart:
```
> Tee
> Coffee
<
> Add sugar
> Add milk
```
Will first present `Tee` and `Coffee`, and once either of these have been chosen, the second set of choices will
be presented, `Add sugar` and `Add milk`.
Without `<` all four of them would be presented at the same time.

#### Incrementation
The system supports spaces and tabs but you must stick to either one throughout the same file,
the first incremented line in the file defines which character and how wide each level of incrementation should be.

See [>](#``>``) for incrementation uses.

#### Tags
This system supports any of the HTML tags TextMeshPro supports, place `<b>` and `<\b>` around text to make it bold.

See [TextMeshPro Documentation](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.2/manual/RichText.html)

#### Example
Here's a fairly short example showing how the syntax works.
No commands or rich tags are used here, see [usage](#usage) for more.
```
========= Start =
The great city of Maar stands before you.
Other travelers huddle around the city's gate.
> Go through the gate
	-> Gate
> Approach the caravan
	-> Merchant

==== Merchant =
You walk up to the large caravan, an old kobold opens the door and seeing you, waves you in.
Oh, hello stranger ! Welcome to Jebra's Emporium !
You step into the caravan while the old kobold scurries behind the counter.
So, what can I do for you ? He points to the items laid out next to him.
There's a lot of miscellaneous items strewn about, you notice a tiny kobold statue sitting on the counter
> Inspect the kobold statue
	You stare at the tiny statue, its gaze shifts towards yours, it starts laughing and jumps off the counter.
	Oh you ! What are you still doing here Ramji !
	The old kobold spouts while the child scurries out.
	Go help your brothers out, will you !
	Jebra turns back to you and smiles.
	Sorry about that !
	-> Merchant
> Exit the caravan
	You move towards the door.
	Come again !
	-> Start

==== Gate ==
A guard stands before the gate, his name plate reads "Immigration Officer Marmo".
Oi anything you're carrying I would like to know ?
> Nope sir, I swear on my mum
	That's what I like to hear. ... Well, go on then ! I've still got yer scaly friends to deal with.
	He gestures behind you to a group of kobolds unloading their caravan.
	-> Maar
> Uuh, I'll be right back !
	-> Start

== Maar =

```
