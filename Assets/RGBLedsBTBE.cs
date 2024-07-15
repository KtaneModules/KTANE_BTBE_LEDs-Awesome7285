using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class RGBLedsBTBE : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public KMSelectable LeftButton;
   public KMSelectable RightButton;
   public KMSelectable SubmitButton;
   public TextMesh DisplayText;
   public MeshRenderer[] LEDS;
   public Material[] LEDMaterials;

   string SolveSound = "snd_disarm";

   string[] Frequencies = { "98.43", "98.48", "98.53", "98.63", "98.68", "98.78", "98.83", "98.88" };
   string[] LEDColours = { "Blue", "Cyan", "Green", "Magenta", "Red", "Yellow", "Black", "White" };

   string CorrectFrequency = "";
   int CurrentSelection = 0;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;
      /*
      foreach (KMSelectable object in keypad) {
          object.OnInteract += delegate () { keypadPress(object); return false; };
      }
      */

      LeftButton.OnInteract += delegate () { LeftButtonPress(LeftButton); return false; };
      RightButton.OnInteract += delegate () { RightButtonPress(RightButton); return false; };
      SubmitButton.OnInteract += delegate () { SubmitButtonPress(SubmitButton); return false; };
   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      DisplayText.text = Frequencies[0];

      // Randomize colours, no duplicates
      int LED0Num = Rnd.Range(0, LEDMaterials.Length);
      int LED1Num = Rnd.Range(0, LEDMaterials.Length);
      while (LED1Num == LED0Num) {
         LED1Num = Rnd.Range(0, LEDMaterials.Length);
      }
      int LED2Num = Rnd.Range(0, LEDMaterials.Length);
      while (LED2Num == LED1Num || LED2Num == LED0Num) {
         LED2Num = Rnd.Range(0, LEDMaterials.Length);
      }

      string[] LEDChosenColours = { LEDColours[LED0Num], LEDColours[LED1Num], LEDColours[LED2Num] };
      Debug.LogFormat("[RGB Mixing #{0}] Generated LED Colours: {1}, {2}, {3}.", ModuleId, LEDChosenColours[0], LEDChosenColours[1], LEDChosenColours[2]);

      LEDS[0].material = LEDMaterials[LED0Num];
      LEDS[1].material = LEDMaterials[LED1Num];
      LEDS[2].material = LEDMaterials[LED2Num];

      int[] LED0Colours = { -1, -1, -1 };
      int[] LED1Colours = { -1, -1, -1 };
      int[] LED2Colours = { -1, -1, -1 };
      int[][] LEDSRGB = { LED0Colours, LED1Colours, LED2Colours };

      for (int i = 0; i < 3; i++) {
         switch (LEDChosenColours[i]) {
            case "Red": 
               LEDSRGB[i] = new int[3] {1, 0, 0};
               break;
            case "Green": 
               LEDSRGB[i] = new int[3] {0, 1, 0};
               break;
            case "Blue": 
               LEDSRGB[i] = new int[3] {0, 0, 1};
               break;
            case "Cyan": 
               LEDSRGB[i] = new int[3] {0, 1, 1};
               break;
            case "Magenta": 
               LEDSRGB[i] = new int[3] {1, 0, 1};
               break;
            case "Yellow": 
               LEDSRGB[i] = new int[3] {1, 1, 0};
               break;
            case "White": 
               LEDSRGB[i] = new int[3] {1, 1, 1};
               break;
            case "Black": 
               LEDSRGB[i] = new int[3] {0, 0, 0};
               break;
            default: 
               LEDSRGB[i] =  new int[3] {0, 0, 0};
               break;
         }
      }

      int CorrectLED = -1;

      // Start of Conditional Rules from manual

      // If all three LEDs have green values of 1, use the first LED.
      if (LEDSRGB[0][1] == 1 && LEDSRGB[1][1] == 1 && LEDSRGB[2][1] == 1) {
         CorrectLED = 0;
         Debug.LogFormat("[RGB Mixing #{0}] Rule 1 Applies. Correct LED is #{1}.", ModuleId, CorrectLED+1);
      }
      // Otherwise, if all three LEDs have red values of 0, use the third LED.
      else if (LEDSRGB[0][0] == 0 && LEDSRGB[1][0] == 0 && LEDSRGB[2][0] == 0) {
         CorrectLED = 2;
         Debug.LogFormat("[RGB Mixing #{0}] Rule 2 Applies. Correct LED is #{1}.", ModuleId, CorrectLED+1);
      }
      // Otherwise, if there is only one LED that has a blue value of 1, use the LED with the blue value of 1.
      else if (LEDSRGB[0][2] + LEDSRGB[1][2] + LEDSRGB[2][2] == 1) {
         for (int i = 0; i < 3; i++) {
            if (LEDSRGB[i][2] == 1) {
               CorrectLED = i;
            }
         }
         Debug.LogFormat("[RGB Mixing #{0}] Rule 3 Applies. Correct LED is #{1}.", ModuleId, CorrectLED+1);
      }
      // Otherwise, if there is only one LED that has a green value of 0, use the LED with the green value of 0.
      else if (LEDSRGB[0][1] + LEDSRGB[1][1] + LEDSRGB[2][1] == 2) {
         for (int i = 0; i < 3; i++) {
            if (LEDSRGB[i][1] == 0) {
               CorrectLED = i;
            }
         }
         Debug.LogFormat("[RGB Mixing #{0}] Rule 4 Applies. Correct LED is #{1}.", ModuleId, CorrectLED+1);
      }
      // Otherwise, use the second LED.
      else {
         CorrectLED = 1; // Using the second LED
         Debug.LogFormat("[RGB Mixing #{0}] Otherwise condition Applies. Correct LED is #{1}.", ModuleId, CorrectLED+1);
      }

      // Start of Step 2 in Manual

      string CorrectColour = LEDChosenColours[CorrectLED];

      switch (CorrectColour) {
         case "White":
            CorrectFrequency = "98.43";
            break;
         case "Cyan":
            CorrectFrequency = "98.48";
            break;
         case "Red":
            CorrectFrequency = "98.53";
            break;
         case "Green":
            CorrectFrequency = "98.63";
            break;
         case "Magenta":
            CorrectFrequency = "98.68";
            break;
         case "Yellow":
            CorrectFrequency = "98.78";
            break;
         case "Black":
            CorrectFrequency = "98.83";
            break;
         case "Blue":
            CorrectFrequency = "98.88";
            break;
      }

      Debug.LogFormat("[RGB Mixing #{0}] The correct colour is {1}. Making the correct frequency {2}.", ModuleId, CorrectColour, CorrectFrequency);

   }

   void LeftButtonPress (KMSelectable LeftButton) {
      LeftButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, LeftButton.transform);
      if (ModuleSolved) {
         // Do nothing if module is already solved
         return;
      }
      // Scroll Left
      if (CurrentSelection > 0) {
         CurrentSelection--;
         DisplayText.text = Frequencies[CurrentSelection];
      }
   }

   void RightButtonPress (KMSelectable RightButton) {
      RightButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, LeftButton.transform);
      if (ModuleSolved) {
         // Do nothing if module is already solved
         return;
      }
      // Scroll Right
      if (CurrentSelection < Frequencies.Length-1) {
         CurrentSelection++;
         DisplayText.text = Frequencies[CurrentSelection];
      }
   }

   void SubmitButtonPress (KMSelectable SubmitButton) {
      // Submit le module
      if (ModuleSolved) {
         // Do nothing if module is already solved
         return;
      }
      if (Frequencies[CurrentSelection] == CorrectFrequency) {
         // Solve
         GetComponent<KMBombModule>().HandlePass();
         ModuleSolved = true;
         Debug.LogFormat("[RGB Mixing #{0}] You submitted {1}. That is correct!", ModuleId, Frequencies[CurrentSelection]);
         Audio.PlaySoundAtTransform(SolveSound, SubmitButton.transform);
         // Set colour to black for souvenir support?
         // Souv would have to ask about an LED that wasn't the solution since the freq display won't change (it'll prob look bad)
         LEDS[0].material = LEDMaterials[6];
         LEDS[1].material = LEDMaterials[6];
         LEDS[2].material = LEDMaterials[6];
      } else {
         // Strike
         GetComponent<KMBombModule>().HandleStrike();
         Debug.LogFormat("[RGB Mixing #{0}] You submitted {1}. But I was expecting {2}", ModuleId, Frequencies[CurrentSelection], CorrectFrequency);
      }
   }

   void OnDestroy () { //Shit you need to do when the bomb ends
      
   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on

   }

   void Update () { //Shit that happens at any point after initialization

   }

   void Solve () {
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike () {
      GetComponent<KMBombModule>().HandleStrike();
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} submit <frequency> to submit a frequency.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      yield return null;
      string[] Commands = Command.Split(' ');
      // Command is not a submit
      if (Commands[0] != "SUBMIT") {
         yield return "sendtochaterror Invalid Command.";
         yield break;
      }
      // If frequency is not valid
      if (!Frequencies.Contains(Commands[1])) {
         yield return "sendtochaterror Invalid Frequency Value.";
         yield break;
      }
      // Scroll to submission
      // Check if value is smaller or greater to decide if scrolling left or right
      int index = Array.IndexOf(Frequencies, Commands[1]);
      int TimesToMove = index - CurrentSelection;

      if (TimesToMove > 0) {
         for (int i=0; i < TimesToMove; i++) {
            RightButton.OnInteract();
            yield return new WaitForSeconds(.05f);
         }
      } else {
         for (int i=0; i < TimesToMove*-1; i++) {
            LeftButton.OnInteract();
            yield return new WaitForSeconds(.05f);
         }
      }

      // Submit
      yield return new WaitForSeconds(.1f);
      SubmitButton.OnInteract();
   }

   IEnumerator TwitchHandleForcedSolve () {
      // Scroll to submission
      // Check if value is smaller or greater to decide if scrolling left or right
      int index = Array.IndexOf(Frequencies, CorrectFrequency);
      int TimesToMove = index - CurrentSelection;

      if (TimesToMove > 0) {
         for (int i=0; i < TimesToMove; i++) {
            RightButton.OnInteract();
            yield return new WaitForSeconds(.05f);
         }
      } else {
         for (int i=0; i < TimesToMove*-1; i++) {
            LeftButton.OnInteract();
            yield return new WaitForSeconds(.05f);
         }
      }

      // Submit
      yield return new WaitForSeconds(.1f);
      SubmitButton.OnInteract();
   }
}
