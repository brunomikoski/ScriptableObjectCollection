# Scriptable Object Collection

<p align="center">
    <a href="https://github.com/brunomikoski/ScriptableObjectCollection/blob/master/LICENSE.md">
		<img alt="GitHub license" src ="https://img.shields.io/github/license/Thundernerd/Unity3D-PackageManagerModules" />
	</a>

</p> 
<p align="center">
	<a href="https://www.codacy.com/gh/brunomikoski/ScriptableObjectCollection/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=brunomikoski/ScriptableObjectCollection&amp;utm_campaign=Badge_Grade"><img src="https://app.codacy.com/project/badge/Grade/468941ad106648fc952ee1871840af9f"/></a>
    <a href="https://openupm.com/packages/com.brunomikoski.scriptableobjectcollection/">
        <img src="https://img.shields.io/npm/v/com.brunomikoski.scriptableobjectcollection?label=openupm&amp;registry_uri=https://package.openupm.com" />
    </a>

  <a href="https://github.com/brunomikoski/ScriptableObjectCollection/issues">
     <img alt="GitHub issues" src ="https://img.shields.io/github/issues/brunomikoski/ScriptableObjectCollection" />
  </a>

  <a href="https://github.com/brunomikoski/ScriptableObjectCollection/pulls">
   <img alt="GitHub pull requests" src ="https://img.shields.io/github/issues-pr/brunomikoski/ScriptableObjectCollection" />
  </a>
  
  <img alt="GitHub last commit" src ="https://img.shields.io/github/last-commit/brunomikoski/ScriptableObjectCollection" />
</p>

<p align="center">
    	<a href="https://github.com/brunomikoski">
        	<img alt="GitHub followers" src="https://img.shields.io/github/followers/brunomikoski?style=social">
	</a>	
	<a href="https://twitter.com/brunomikoski">
		<img alt="Twitter Follow" src="https://img.shields.io/twitter/follow/brunomikoski?style=social">
	</a>
</p>



Most of the time when dealing with Scriptable Objects they all belong to some sort of collection, let's say for example all your consumables on the game? Or maybe all your weapons? Or even all your in-app purchases. Dealing with this can be quite challenging since you have to rely on proper naming of those scriptable objects, this can become a problem super fast as the project continues to grow.

The ScriptableObjectCollection exists to help you deal with scriptable objects without losing your sanity! It's a set of tools that will make your life a lot easier.


![wizard](/Documentation~/create-collection-wizzard.png)
![Static access to your items](https://github.com/brunomikoski/ScriptableObjectCollection/blob/master/Documentation~/code-access.gif)
![DropDown for selecting Collectable Values](https://github.com/brunomikoski/ScriptableObjectCollection/blob/master/Documentation~/property-drawer.gif)

Check the [FAQ](https://github.com/brunomikoski/ScriptableObjectCollection/wiki/FAQ) with more examples and use examples.


## Features
- Allow access Scriptable Objects by code, reducing the number of references on the project
- Group Scriptable Objects that belong together in a simple coherent interface
- Enable a dropdown selection of all the items inside a collection when the item is serialized through the inspector
- Automatically generate static access code
- Allow you to expose the entire object to be tweakable in any inspector
- Makes the usability of Scriptable Objects in bigger teams a lot better
- Iterate over all the items of any collection by `Collection.Values`
- If you are using the Static Access to the files, if any of the items goes missing, you will have an editor time compilation error, super easy to catch and fix it.


## How to use
 1. Create new collections by the wizard `Assets/Create/Scriptable Object Collection/New Collection` 
 2. Now you should treat your new `ScriptableObjectCollection` as a regular `ScriptableObject`, add any item you want there  
 3. Now add new items to the collection by using the buttons on the Collection Inspector
 4. After you are done, click on Generate Code on the collection to generate the Static access to those objects


## [FAQ]
<details>
  <summary>I'm having issues when deleting duplicating items</summary>
 It's really hard to make sure those features work perfectly with the system since it depends on to catch up the GUIDs of the collectables, **ALWAYS** try to use the Add New / Deleting by the inspector itself
</details>

<details>
  <summary>How I can propery serialize this for Save Game or Backend data</summary>
There's a couple of ways of dealing with this, the easiest one is to use the `IndirectReference` when you need this data to be serialized, this will only store 2 GUIDs.
 Another option is writing a proper parser using the ISerializationCallback.
</details>

<details>
  <summary>Collection Registry on the Resources Folder</summary>
Since the CollectionsRegistry is inside the Resources folder, every reference this has to a collection and to all the collectables will be inside the Unity Resources bundle, and if you have a lot of references for expensive stuff, this can increase your startup time significantly, there are 2 things you should keep in mind: 
1. Use the Automatically Loaded items for items that should be available for the lifetime of your game
2. If you want to use this for more expensive stuff, let's say all the gameplay prefabs, you can uncheck the automatic initialization of this collection, and register the collection on your loading by using `CollectionsRegistry.Instance.RegisterCollection(Collection);` and removing it when they are not necessary anymore.
3. You can convert the items of a collection to references by using the "Convert To References" button in the collection inspector. This allows for more flexibility to load and unload items individually. These items are inserted into an Addressables group.
</details>
 
 <details>
  <summary>How can I create a new collection</summary>
You can use the Collection Creating Wizard by right click on the project panel and going into: `Assets/Create/Scriptable Object Collection/New Collection` this will create 3 items and respective folder: `YourCollectionName.cs` `YourCollectableName.cs` and the Collection Scriptable Object
![Create Collection Wizard](https://github.com/badawe/ScriptableObjectCollection/blob/master/Documentation~/create-collection-wizzard.png)
</details>
 
 
  <details>
  <summary>Use a direct reference to the item instead of the dropdown</summary>
When you add a reference to a Collectable you may choose how you want this to be displayed by adding `[CollectionItemEditorOptions]`.<br />
There are two Draw Types for now:<br /><br />
	- <b>DropDown</b> the default option which displays all the available options in a convenient drop-down, like an enum field <br />
- <b>AsReference</b> standard Scriptable Object field where you select the asset in a browser <br />
<br />
<img src="https://github.com/badawe/ScriptableObjectCollection/blob/master/Documentation~/property-drawer.gif" />
</details>
 
 
## System Requirements
Unity 2018.4.0 or later versions


## How to install

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.brunomikoski.scriptableobjectcollection

To add the package to your project:

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
