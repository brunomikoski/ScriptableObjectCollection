# Scriptable Object Collection

[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/brunomikoski/ScriptableObjectCollection/blob/develop/LICENSE)
[![openupm](https://img.shields.io/npm/v/com.brunomikoski.scriptableobjectcollection?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.scriptableobjectcollection/) [![Codacy Badge](https://app.codacy.com/project/badge/Grade/177397001d74494a9ec54031a428c8dc)](https://www.codacy.com/manual/badawe/ScriptableObjectCollection?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=badawe/ScriptableObjectCollection&amp;utm_campaign=Badge_Grade)

[![](https://img.shields.io/github/followers/brunomikoski?label=Follow&style=social)](https://github.com/brunomikoski) [![](https://img.shields.io/twitter/follow/brunomikoski?style=social)](https://twitter.com/brunomikoski)


Most of the time when dealing with Scriptable Object they all belong to some sort of collections, let's say for example all your consumables on the game? Or maybe all your weapons? Or even all your in-app purchases. And dealing with this can be quite challenging since you have to rely on proper naming of those scriptable objects, this can become a problem super fast as the project continues to grow.

The ScriptableObjectCollection exists to help you deal with scriptable objects without losing your sanity! Its a set of tools that will make your life a lot easier.


![wizard](/Documentation~/create-collection-wizzard.png)
![Static access to your items](https://github.com/badawe/ScriptableObjectCollection/blob/master/Documentation~/code-access.gif)
![DropDown for selecting Collectable Values](https://github.com/badawe/ScriptableObjectCollection/blob/master/Documentation~/property-drawer.gif)

Check the [FAQ](https://github.com/badawe/ScriptableObjectCollection/wiki/FAQ) with more examples and use examples.


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


## [FAQ](https://github.com/badawe/ScriptableObjectCollection/wiki/FAQ)
 
 
## System Requirements
Unity 2018.4.0 or later versions


## Installation

### OpenUPM
The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.brunomikoski.scriptableobjectcollection
```

### Manifest
You can also install via git URL by adding this entry in your **manifest.json**
```
"com.brunomikoski.scriptableobjectcollection": "https://github.com/badawe/ScriptableObjectCollection.git"
```

### Unity Package Manager
```
from Window->Package Manager, click on the + sign and Add from git: https://github.com/badawe/ScriptableObjectCollection.git
```

## License TL:DR
- You can freely use Scriptable Object Collection in both commercial and non-commercial projects
- You can redistribute verbatim copies of the code, along with any readme files and attributions
- You can modify the code only for your own (company/studio) use and you cannot redistribute modified versions outside your own company/studio (but you can send pull requests to me)

