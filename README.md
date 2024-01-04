# VAwait
 Compact and simple pooled async/await library for Unity3D.  
# Installation  
- Download the source(.zip) and extract it to your Assets folder in your project.
- Add the namespace in your script `using VAwait;`  
# Syntax  
```
//Wait for seconds
await Wait.Seconds(5f);

//Wait for frames
await Wait.NextFrame();

//Wait for Coroutines
await Coroutine(MyCoroutine());

//Canceling an await
var wait = Wait.NextFrame();
await wait;
wait.Cancel();

//Destroy the awaits
//Use this on ApplciationQuit or when the quitting your game.
Wait.DestroyAwaits();
```
