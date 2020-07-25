# Scriptable Object Collection


[![openupm](https://img.shields.io/npm/v/com.brunomikoski.scriptableobjectcollection?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.scriptableobjectcollection/)

![](https://img.shields.io/github/followers/badawe?label=Follow&style=social) ![](https://img.shields.io/twitter/follow/brunomikoski?style=social)


Scriptable Object Collection its a tool to help you work with scriptable objects, by allowing static code access to any of the Assets while keeping the usability of having a configurable by Inspector asset.


![wizard](/Documentation~/create-collection-wizzard.png)
*Create new Collections by the Collection Creation Wizzard*

![collection-usability](/Documentation~/collection-usability.gif) 
*Automatically Add New, delete and manage order of items*

![wizard](/Documentation~/property-drawer.gif)
*Expose items to be selected from a dropdown, including an inline editor for the CollectableScriptableObject*

![code-access](/Documentation~/code-access.gif) 
*Access all the ScriptableObjects by code by the automatic generated static access*



## Features
- Allow access Scriptable Objects by code, reducing the number of references on the project
- Group Scriptable Objects that bellows together in a simple coherent interface
- Enable a dropdown selection of all the items inside a collection when the item is serialized through the inspector
- Automatically generate static access code
- Allow you to expose the entire object to be tweakable in any inspector
- Makes the usability of Scriptable Objects in bigger teams a lot better
- Iterate over all the items of any collection by `Collection.Values`


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

