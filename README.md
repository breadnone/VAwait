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
await Wait.Coroutine(MyCoroutine());

//Canceling an await
var wait = Wait.NextFrame();
await wait;
wait.Cancel();

//Runs on threadPool
Wait.RunOnThreadpool(()=>{Debug.Log("In a threadPool.");});

//Or awaits the threadPool
await Wait.RunOnThreadpool(()=>{Debug.Log("In a threadPool.");});

ThreadPool ==========================================

//Back to mainthread from threadPool
await Wait.RunOnThreadpool(()=>{Debug.Log("In a threadPool.");});

//from here we're in a threadPool userland
//To switch back to mainthread, we can use BeginInvokeOnMainthread. see below.

await Wait.BeginInvokeOnMainthread(()=>{Debug.Log("Back to Mainthread");});
//We are now back in the mainthread

```
