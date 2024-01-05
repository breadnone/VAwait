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

   //Wait for FixedUpdate
   await Wait.FixedUpdate();
   
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
 
     //Use frame.Cancel() or second.Cancel() to cancel based on the example above.
     //Note: Once Reusables are cancelled, they can't be awaited.
   }

   //Canceling an await
   await Wait.NextSeconds(10f, setId: 2);
   Wait.TryCancel(2);
}
```
# Note :  
- The SignalAwaiter instances can't be awaited multiple times. Use NextFrameReusable or SecondsReusable for when you need to await it more than once.
- Runtime only.
