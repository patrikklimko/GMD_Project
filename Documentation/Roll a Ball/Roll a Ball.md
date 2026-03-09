````md
# Roll a Ball: How We Expanded the Game

For our Game Development course, we started with Unity’s classic **Roll-a-Ball** tutorial, and then we pushed it a bit further so it feels more like an actual mini game instead of just a demo scene. The goal was to keep the core mechanic (rolling around with physics), but add small systems that show we understand Unity basics like **audio, UI, triggers, collisions, and simple animations**.

---

## 1) Adding Sound Effects

The tutorial version is kinda silent, so the first thing we added was sound feedback. Even basic audio makes it feel way more “real” because you instantly know when something happened (pickup, door unlock, losing, etc.).

We added different sound effects for:
- collecting a pickup
- opening/unlocking the door
- losing / game over (when hitting an enemy)

In Unity, the simplest setup is:
- Store the sounds as `AudioClip`
- Play them using an `AudioSource` component
- Trigger sounds from code when events happen

### Audio setup in Unity
We attached an `AudioSource` component to the player, and then exposed `AudioClip` fields in the `PlayerController` script so we can assign sounds directly from the Inspector without hardcoding paths.

Example fields we used:

```csharp
[SerializeField] private AudioClip gameOverAudio;
[SerializeField] private AudioClip pickUpAudio;
````

The nice part about this is: if we later want to change sounds, we don’t touch code — just drag a new clip into the Inspector.

---

## 2) Playing a Sound When Collecting a Pickup

Pickups are handled using triggers. So whenever the player touches something tagged `"Pickup"`, we:

1. disable the pickup object
2. increase the pickup counter
3. update UI text
4. play the pickup sound

<img width="661" height="884" alt="image" src="https://github.com/user-attachments/assets/3c163b01-e5c0-444e-8cae-698f6938caf6" />


Code idea (same logic as we used):

```csharp
private void OnTriggerEnter(Collider other)
{
    if (other.gameObject.CompareTag("Pickup"))
    {
        other.gameObject.SetActive(false);
        _count += 1;
        SetCountText();

        _audioSource.clip = pickUpAudio;
        _audioSource.Play();
    }
}
```

This is simple, but it’s one of those patterns you use everywhere in Unity.

---

## 3) Playing a Sound When the Player Loses

For losing, we used collisions (not triggers), because we want the enemy to physically collide with the player.

When the player hits an object tagged `"Enemy"`:

* We play the “game over” sound
* We show a lose UI message
* We destroy the player after the sound finishes (so the sound doesn’t get cut off instantly)

(PICTURE WITH GAME OVER CODE 2)

Example structure:

```csharp
private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Enemy"))
    {
        _audioSource.clip = gameOverAudio;
        _audioSource.Play();

        Destroy(gameObject, gameOverAudio.length);

        winTextObject.gameObject.SetActive(true);
        winTextObject.GetComponent<TMPro.TextMeshProUGUI>().text = "You Lose!";
    }
}
```

This was a small detail, but it actually makes the game feel smoother.

---

## 4) Expanding the Level

The original Roll-a-Ball level is basically “a box with pickups”. We wanted the level to have at least some structure so it feels more like a playable map.

What we changed:

* Added corridors and turns so the player has to navigate
* Added different elevations (ramps / platforms)
* Spread pickups so it encourages exploration
* Used walls to make it feel like a real arena instead of an empty floor

(PICTURE WITH EXPANDED LEVEL OVERVIEW 3)

Even though we didn’t change the main mechanic, changing the level design alone made the gameplay feel way more engaging.

---

## 5) Adding a Simple Unlock-able Door (Mini Objective)

To add a bit of progression, we introduced a locked door that only opens after collecting enough pickups. This gives the player a clear goal:

> collect enough → unlock door → reach next area / finish

We implemented this using:

* a `DoorController` script
* Unity’s `Animator` system
* a trigger parameter to start the door opening animation

(PICTURE WITH DOOR CONTROLLER SCRIPT 4)

Example door controller:

```csharp
public class DoorController : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void UnlockDoor()
    {
        _animator.SetTrigger("openDoor");
    }
}
```

---

## 6) Animator State Machine for the Door

The animation logic is handled inside the Animator:

* The door starts in an `Idle` state
* When we set the `openDoor` trigger, it transitions to `DoorOpening`
* The animation plays once and the door is now open

(PICTURE WITH ANIMATOR STATE MACHINE 5)

This approach is clean because:

* the script doesn’t manually move the door
* the Animator owns the animation timeline
* the script just sends an “event” (trigger)

---

## What I Learned

This small expansion taught me a bunch of “real Unity basics”:

* how to use triggers vs collisions
* how to add audio properly without hardcoding
* how to connect UI updates with gameplay events
* how to use Animator for simple interactables
* how small level design changes can massively improve the game feel

```
```
