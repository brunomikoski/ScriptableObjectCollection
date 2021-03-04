# Scriptable Object Collection

[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/brunomikoski/ScriptableObjectCollection/blob/develop/LICENSE)
[![openupm](https://img.shields.io/npm/v/com.brunomikoski.scriptableobjectcollection?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.scriptableobjectcollection/) [![Codacy Badge](https://app.codacy.com/project/badge/Grade/177397001d74494a9ec54031a428c8dc)](https://www.codacy.com/manual/badawe/ScriptableObjectCollection?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=badawe/ScriptableObjectCollection&amp;utm_campaign=Badge_Grade)

[![](https://img.shields.io/github/followers/brunomikoski?label=Follow&style=social)](https://github.com/brunomikoski) [![](https://img.shields.io/twitter/follow/brunomikoski?style=social)](https://twitter.com/brunomikoski)


Most of the time when dealing with Scriptable Object they all belong to some sort of collections, let's say for example all your consumables on the game? Or maybe all your weapons? Or even all your in-app purchases. And dealing with this can be quite challenging since you have to rely on proper naming of those scriptable objects, this can become a problem super fast as the project continues to grow.

The ScriptableObjectCollection exists to help you deal with scriptable objects without losing your sanity! Its a set of tools that will make your life a lot easier.


![wizard](/Documentation~/create-collection-wizzard.png)
![Static access to your items](https://github.com/brunomikoski/ScriptableObjectCollection/blob/master/Documentation~/code-access.gif)
![DropDown for selecting Collectable Values](https://github.com/brunomikoski/ScriptableObjectCollection/blob/master/Documentation~/property-drawer.gif)

Check the [FAQ](https://github.com/brunomikoski/ScriptableObjectCollection/wiki/FAQ) with more examples and use examples.


## Features
- Allow access Scriptable Objects by code, reducing the number of references on the project
- Group Scriptable Objects that bellows together in a simple coherent interface
- Enable a dropdown selection of all the items inside a collection when the item is serialized through the inspector
- Automatically generate static access code
- Allow you to expose the entire object to be tweakable in any inspector
- Makes the usability of Scriptable Objects in bigger teams a lot better
- Iterate over all the items of any collection by `Collection.Values`
- If you are using the Static Access to the files, if any of the items goes missing, you will have an editor time compilation error, super easy to catch and fix it.


## How to use
 1. Create new collections by the wizard `Assets/Create/Scriptable Object Collection/New Collection` 
 2. Now you should treat your new `ScriptableObjectCollection` as a regular `ScriptableObject`, add any item you wan there  
 3. Now add new items to the collection by using the buttons on the Collection Inspector
 4. After you are done, click on Generate Code on the collection to generate the Static access to those objects


## [FAQ](https://github.com/brunomikoski/ScriptableObjectCollection/wiki/FAQ)
 
 
## System Requirements
Unity 2018.4.0 or later versions


## How to install

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.brunomikoski.scriptableobjectcollection

To add it the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.brunomikoski
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.brunomikoski.scriptableobjectcollection`
- click <kbd>Add</kbd>
</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates :( </em></summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/brunomikoski/ScriptableObjectCollection.git`
- click <kbd>Add</kbd>
</details>
