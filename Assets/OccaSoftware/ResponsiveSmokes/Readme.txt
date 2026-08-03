README
================================

Contents
--------------------------------
User Manual
About
Installation Instructions
Usage Instructions
Requirements
Support

User Manual
--------------------------------
Manual: https://occasoftware.com/manual/responsive-smokes



About
--------------------------------
Interactive Volumetric Smoke enables you to render dynamic, interactive volumetric smoke effects in your project.
The smoke is rendered using state-of-the-art volumetric rendering methods.

The smoke propagates dynamically in your scene in response to your scene geometry.
You can interact with the smoke using projectiles or explosions.

There is a detailed demo scene included in the project. This demo scene showcases how you could implement the smoke system in your game.

You can configure the smoke quality, appearance, and generation settings.




Installation Instructions
--------------------------------
1. Interactive Volumetric Smoke into your project.
2. Set the Interactive Smoke prefab to a new Layer. I named mine "InteractiveSmoke".
3. Set the Layer Masks on the Interactive Projectile and Interactive Explosion prefabs.
 3a. The "Interactive Smoke Layer" should match the layer you have applied to your Interactive Smoke prefab.
 3b. The "Blocking Layer Mask" in the projectile should match any layers that you want to block projectiles.
4. Explore the demo scene.


Usage Instructions
--------------------------------
The Interactive Smoke, Interactive Projectile, and Interactive Explosion components will each automatically execute when spawned.
Their execute method is called in the Start() method.

To trigger an effect, you should Instantiate an instance of the prefab at the intended position.

The Interactive Smoke effect should never be rotated.
The Interactive Projectile effect should be rotated so that the transform.forward points towards the direction of travel.

The lifetime of each effect is equal to the In + Lifetime + Out durations.

One smoke can account for up to 10 projectiles and up to 3 explosions at one time. These are separate limits.
If you try to add additional projectiles and/or explosions at this stage, the new projectiles and/or explosions will be ignored.

All of the Smoke settings are controlled from the Interactive Smoke component.
All of the Projectile settings are controlled from the Interactive Projectile component.
All of the Explosion settings are controlled from the Interactive Explosion component.

You can create one prefab for each preset that you want.
Multiple unique Projectile and Explosion prefabs can interact with a single Smoke prefab at the same time.


Requirements
--------------------------------
This asset is compatible with URP.
Incompatible with VR.
Incompatible with Orthographic view.
Not recommended for mobile.


Support
--------------------------------
If you're not happy, I'm not happy.
Please contact us at hello@occasoftware.com or join our Discord @ https://www.occasoftware.com/discord for any support.

