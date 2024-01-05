# VAwait
 Compact and simple pooled async/await library for Unity3D.  
# Installation  
- Download the source(.zip) and extract it to your Assets folder in your project.
- Add the namespace in your script `using VAwait;`  
# Syntax  
```
async Task AsyncMethod()
{
   //Wait for seconds
   await Wait.Seconds(5f);
   
   //Wait for frames
   await Wait.NextFrame();
   
   //Wait for Coroutines
   await Wait.Coroutine(MyCoroutine());

  //Awaiting more than once.
  //By default the SignalAwaiter can't do this due to object pooling.
  //We can use the SignalAwaiterReusable for this use case.

  var tokenSource = new CancellationTokenSource();
  var frame = Wait.NextFrameReusable();
  var second = Wait.SecondsReusable(tokenSource); // Token source must be passed for canceling purposes

  while(true)
  {
    //Will be reusing the same instance or awaited multiple times.
    await frame;
    await second;
  }
   
   //Canceling an await
   await Wait.NextSeconds(10f, setId: 2);
   Wait.Cancel(2);

ThreadPool ==========================================
   
   //Runs on threadPool
   Wait.RunOnThreadpool(()=>{Debug.Log("In a threadPool.");});
   
   //Or awaits the threadPool
   await Wait.RunOnThreadpool(()=>{Debug.Log("In a threadPool.");});
   
   //from here we're in a threadPool userland
   //To switch back to mainthread, we can use BeginInvokeOnMainthread. see below.
   
   await Wait.BeginInvokeOnMainthread(()=>{Debug.Log("Back to Mainthread");});
   //We are now back in the mainthread

}
```
# Note :  
- The SignalAwaiter instances can't be awaited multiple times. Use NextFrameReusable or SecondsReusable for when you need to await it more than once.
- Runtime only.
