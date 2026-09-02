using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;
using KModkit;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

//The module will try to work with two variables. There will be three lines for each variable, making six lines in total: a declaration line, an assignment line and a read line.
//I refer to the set of three lines that deal with each variable as a "division".
//A seventh line that tries to add the two variables together has a 50% chance of appearing.
[DisallowMultipleComponent]
public class HelloWorldScript : MonoBehaviour {
	public KMAudio Audio;
	public KMBombModule Module;
	public KMRuleSeedable RuleSeedable;
	public KMModSettings ModSettings;
	public KMColorblindMode colorblindMode;
	static int moduleIdCounter = 1;
	int moduleId;

	public KMSelectable[] Buttons;
	public Renderer[] Leds;
	public Renderer[] Plodedleds;
	public Renderer[] ColourblindLabels;
	public Material[] ColourblindMats;
	public GameObject[] Legs;
	public Renderer[] ButtonRenderers;
	public Material[] ButtonMats;
	public Renderer[] WritingRenderers;
	public Material[] WritingMats;
	Material[] ButtonColours = new Material[4];
	Material[] Writings = new Material[4];
	public Light[] Halos;
	Vector3 position;
	Coroutine[] PlodeCoroutines = new Coroutine[4];
	Coroutine CheckCoroutine = null;
	public HelloWorldSettings Settings = new HelloWorldSettings{};
	int[] ButtonStates = new int[4];
	public TextMesh ScreenText;
	string[] StartText = new string[4];

	int ExtraSemicolons = 0;
	int MissingSemicolons = 0;
	int MisspelledKeywords = 0;
	int TypeMismatches = 0;
	bool AssignedBeforeDeclared = false;
	bool ReadBeforeDeclared = false;
	bool ReadBeforeAssigned = false;

	int[] FlipCount = new int[]{0, 0, 0, 0};

	//In each subarray, index 0 = supposed, index 1 = alternate
	string[][] SelectedVarNames = new string[][]{
		new string[]{ "",""},
		new string[]{ "",""}};
	
	enum types
	{
		@int, @string
	}
	
	//0 = int, 1 = string
	types[] DivisionTypes = new types[2];

	string[][] DivisionsText = new string[][]{
		new string[]{ "","",""},
		new string[]{ "","",""}};

	enum lineStatus{
		none, altVar, ignored
	}
	lineStatus[][] LineStatuses = new lineStatus[][]{
		new lineStatus[]{lineStatus.none,lineStatus.none,lineStatus.none},
		new lineStatus[]{lineStatus.none,lineStatus.none,lineStatus.none}};

	//Colours for rich text tags. Defined in separate variables for convenience.
	string end = "</color>";
	string darkblue = "<color=#4c8ac2>";
	string lightblue = "<color=#94d3f3>";
	string green = "<color=#39a692>";
	string yellow = "<color=#d3cc94>";
	string brown = "<color=#8b6651>";
	string mint = "<color=#b5cea8>";
	string grey = "<color=#b4b4b4>";

	string[][] beginningTypos = new string[][] {
	new string[] {"publix","pubic","pulbic","plubic","oublic" },
	new string[] {"calss","calls","cllass","clasls","clsas" },
	new string[] {"viod","coid","vois","voif","viud" }};

	string[][] KeywordsTypos = new string[][] {
	//0.
	new string[] {"Consoul","Cornsole","Ocnsole","Conosle","Cnosole" },
	//1.
	new string[] {"tin","ont","nit","itn","iny" },
	//2.
	new string[] {"sting","sring","srting","strig","strign" },
	//3.
	new string[] {"Systen","Sytsem","Ssytem","Sistem","Ysstem" },
	//4.
	new string[] {"Wrtieline","WritLeine","RwiteLine","WriteLien","WirteLine" } };

	string[][] VarTypos = new string[][] {
	//0.
	new string[] {"myNuber","yourNumber","myNubmer","ymNumber","mNyumber" },
	//1.
	new string[] {"myOthreNumber","myOtherNuber","myOtherMunber","myOtherNumbre","myOtterNumber" },
	//2.
	new string[] {"mySting","mySring","mString","ymString","myStrign" },
	//3.//
	new string[] {"myOtherSting","myOtherThing","myOlderString","myOthjerString","mySecondString"}};

	string[] StringValues = new string[]{
		"Tatterdemalion!","bombmanual.com","Literally blank","Six Seven!","Lit FRK","Chicken Jockey!",
		"Skibidi Rizz","We exploded.","Fog Canyon!","I Ii II I_","Mickey Mouse","Taxi Dispatch","Hold on",
		"Small dog","Scrooge","Soggy Steven","Mrs Peacock","Docsplode","Thoughtfulness","sushi shih tzu",
		"prescriptivist","MAZE GAMER","GJMOY","Submit","POOP","Good luck.","broken display","Grape Jelly",
		"四十七","I'm a teapot","Quarter Note","Twisted Flax","NaOH","Edison Daily","Bacteria","parallel port",
		"Tutti Frutti","Three of a kind"};
	
	//I guess I wanted all possible selected int values to mean something lol
	string[] IntValues = new string[]{"1", "21", "64", "67", "69", "240", "255", "256", "360", "720", "1024", "1080", "1337", "1440", "1920", "2048", DateTime.Now.Year.ToString()};

	string[] beginningWords = new string[] {"public","class","void"};

	//logging class
	public class GenLogEntry
	{
		public int division = 9;
		public int line = 9;
		public string text;
	}


	void Awake()
    {
		moduleId = moduleIdCounter++;
		foreach(KMSelectable button in Buttons)
        {
			button.OnInteract += delegate () { ButtonDown(button); return false; };
			button.OnInteractEnded += delegate () { ButtonUp(button);};
		}
    }

	// Use this for initialization
	void Start () {

		//Logging
		//Each log entry to do with the screen text generation will have 3 elements: division(0/1), line(0/1/2) and text(Example: Misspelled "sting" in), so that the original line can be recovered from the numbers and logged
		// List<GenLogEntry> LoggingMisspelled = new List<GenLogEntry>();
		// List<GenLogEntry> LoggingTypes = new List<GenLogEntry>();
		// List<GenLogEntry> LoggingMissing = new List<GenLogEntry>();
		// List<GenLogEntry> LoggingExtra = new List<GenLogEntry>();
		// List<GenLogEntry> LoggingAssignBeforeDeclare = new List<GenLogEntry>();
		// List<GenLogEntry> LoggingReadBeforeDeclare = new List<GenLogEntry>();
		// List<GenLogEntry> LoggingReadBeforeAssign = new List<GenLogEntry>();

		//Randomise rotation of the exploded LEDs
		foreach(GameObject leg in Legs)
        {
			Vector3 rotation = leg.transform.localRotation.eulerAngles;
			rotation.y = UnityEngine.Random.Range(0, 8) * 45;
			leg.transform.localRotation = Quaternion.Euler(rotation);
		}
		
		//In case the puzzle needs to be regenerated
		ButtonColours = new Material[]{null, null, null, null};
		Writings = new Material[]{null, null, null, null};

		//Arrange the button colours & text randomly
		for (int i = 0; i < 4; i++)
        {
            Material random = ButtonMats[UnityEngine.Random.Range(0, 4)];
            while (ButtonColours.Contains(random))
            {
                random = ButtonMats[UnityEngine.Random.Range(0, 4)];
            }
            ButtonColours[i] = random;

			random = WritingMats[UnityEngine.Random.Range(0, 4)];
			while (Writings.Contains(random))
			{
				random = WritingMats[UnityEngine.Random.Range(0, 4)];
			}
			Writings[i] = random;
		}
		foreach(Renderer button in ButtonRenderers)
        {
			button.material = ButtonColours[Array.IndexOf(ButtonRenderers, button)];
        }
		foreach(Renderer writing in WritingRenderers)
        {
			writing.material = Writings[Array.IndexOf(WritingRenderers, writing)];
        }

		//Colourblind stuff
		for(int i = 0; i < 4; i++)
		{
			ColourblindLabels[i].material = ColourblindMats[Array.IndexOf(ButtonMats, ButtonColours[i])];
			if(!colorblindMode.ColorblindModeActive)
			{
				ColourblindLabels[i].enabled = false;
			}
		}
		

		//Create the screen text
		StartText[1] = "{";
		StartText[3] = "    {";
		for (int i = 0; i < 3; i++)
		{
			if (UnityEngine.Random.Range(0, 3) == 0)
			{
				beginningWords[i] = beginningTypos[i][UnityEngine.Random.Range(0, beginningTypos[i].Length)];
				MisspelledKeywords++;
				Debug.Log($"Misspelled \"{beginningWords[i]}\" at the start. There are now {MisspelledKeywords} misspelled keywords.");
			}
		}
		StartText[0] = $"{darkblue}{beginningWords[0]} {beginningWords[1]}{end} {green}Program{end}";
		StartText[2] = $"{darkblue}{beginningWords[2]}{end} {yellow}Main{end}()";
		for (int i = 0; i < 3; i += 2)
		{
			if (UnityEngine.Random.Range(0, 3) == 0)
			{
				StartText[i] += ";";
				ExtraSemicolons++;
				Debug.Log($"Added an extra semicolon to line {i}. There are now {ExtraSemicolons} extra semicolons.");
			}
		}

		int roll = UnityEngine.Random.Range(0, 6);
		bool firstwrong = false;
		
		//Generate the names of the variables to use
		//am sorry if it pains you to look at code like this LMAO
		//I felt like doing it this way would save me hours of debugging headaches
		switch (roll)
        {
			case 0:
			case 1:
				//Both divisions are ints
				{
					DivisionTypes[0] = types.@int;
					DivisionTypes[1] = types.@int;
					int rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							SelectedVarNames[0][0] = "myString";
							SelectedVarNames[0][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							firstwrong = true;
							break;
						case 1:
							SelectedVarNames[0][0] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							SelectedVarNames[0][1] = "myNumber";
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							SelectedVarNames[0][0] = "myNumber";
							SelectedVarNames[0][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							break;
					}
					rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myOtherString";
								SelectedVarNames[1][1] = VarTypos[3][UnityEngine.Random.Range(0, VarTypos[3].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myString";
								SelectedVarNames[1][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							}
							break;
						case 1:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
								SelectedVarNames[1][1] = "myNumber";
							}
							else
							{
								SelectedVarNames[1][0] = VarTypos[1][UnityEngine.Random.Range(0, VarTypos[1].Length)];
								SelectedVarNames[1][1] = "myOtherNumber";
							}
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myNumber";
								SelectedVarNames[1][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myOtherNumber";
								SelectedVarNames[1][1] = VarTypos[1][UnityEngine.Random.Range(0, VarTypos[1].Length)];
							}
							break;
					}
					break;
				}
            case 2:
            case 3:
				//Both divisions are strings
				{
					DivisionTypes[0] = types.@string;
					DivisionTypes[1] = types.@string;
					int rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							SelectedVarNames[0][0] = "myNumber";
							SelectedVarNames[0][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							firstwrong = true;
							break;
						case 1:
							SelectedVarNames[0][0] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							SelectedVarNames[0][1] = "myString";
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							SelectedVarNames[0][0] = "myString";
							SelectedVarNames[0][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							break;
					}
					rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myOtherNumber";
								SelectedVarNames[1][1] = VarTypos[1][UnityEngine.Random.Range(0, VarTypos[1].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myNumber";
								SelectedVarNames[1][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							}
							break;
						case 1:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
								SelectedVarNames[1][1] = "myString";
							}
							else
							{
								SelectedVarNames[1][0] = VarTypos[3][UnityEngine.Random.Range(0, VarTypos[3].Length)];
								SelectedVarNames[1][1] = "myOtherString";
							}
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myString";
								SelectedVarNames[1][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myOtherString";
								SelectedVarNames[1][1] = VarTypos[3][UnityEngine.Random.Range(0, VarTypos[3].Length)];
							}
							break;
					}
					break;
				}
            case 4:
				//Division 0 is an int, 1 is a string
				{
					DivisionTypes[0] = types.@int;
					DivisionTypes[1] = types.@string;
					int rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							//The variable name pertains to the other type
							SelectedVarNames[0][0] = "myString";
							SelectedVarNames[0][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							firstwrong = true;
							break;
						case 1:
							//The supposed variable has a typo
							SelectedVarNames[0][0] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							SelectedVarNames[0][1] = "myNumber";
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							//correct
							SelectedVarNames[0][0] = "myNumber";
							SelectedVarNames[0][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							break;
					}
					rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myNumber";
								SelectedVarNames[1][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myOtherNumber";
								SelectedVarNames[1][1] = VarTypos[1][UnityEngine.Random.Range(0, VarTypos[1].Length)];
							}
							break;
						case 1:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = VarTypos[3][UnityEngine.Random.Range(0, VarTypos[3].Length)];
								SelectedVarNames[1][1] = "myOtherString";
							}
							else
							{
								SelectedVarNames[1][0] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
								SelectedVarNames[1][1] = "myString";
							}
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myOtherString";
								SelectedVarNames[1][1] = VarTypos[3][UnityEngine.Random.Range(0, VarTypos[3].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myString";
								SelectedVarNames[1][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							}
							break;
					}
					break;
				}
            case 5:
				//Division 0 is a string, 1 is an int
				{
					DivisionTypes[0] = types.@string;
					DivisionTypes[1] = types.@int;
					int rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							//The variable name pertains to the other type
							SelectedVarNames[0][0] = "myNumber";
							SelectedVarNames[0][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							firstwrong = true;
							break;
						case 1:
							//The supposed variable has a typo
							SelectedVarNames[0][0] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							SelectedVarNames[0][1] = "myString";
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							//correct
							SelectedVarNames[0][0] = "myString";
							SelectedVarNames[0][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							break;
					}
					rollTwo = UnityEngine.Random.Range(0, 10);
					switch (rollTwo)
					{
						case 0:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myString";
								SelectedVarNames[1][1] = VarTypos[2][UnityEngine.Random.Range(0, VarTypos[2].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myOtherString";
								SelectedVarNames[1][1] = VarTypos[3][UnityEngine.Random.Range(0, VarTypos[3].Length)];
							}
							break;
						case 1:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = VarTypos[1][UnityEngine.Random.Range(0, VarTypos[1].Length)];
								SelectedVarNames[1][1] = "myOtherNumber";
							}
							else
							{
								SelectedVarNames[1][0] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
								SelectedVarNames[1][1] = "myNumber";
							}
							break;
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
							if (firstwrong)
							{
								SelectedVarNames[1][0] = "myOtherNumber";
								SelectedVarNames[1][1] = VarTypos[1][UnityEngine.Random.Range(0, VarTypos[1].Length)];
							}
							else
							{
								SelectedVarNames[1][0] = "myNumber";
								SelectedVarNames[1][1] = VarTypos[0][UnityEngine.Random.Range(0, VarTypos[0].Length)];
							}
							break;
					}
					break;
				}
		}
        Debug.Log($"Variable types: {DivisionTypes[0]}, {DivisionTypes[1]}. Supposed names:{SelectedVarNames[0][0]}, {SelectedVarNames[1][0]}. Alternate names:{SelectedVarNames[0][1]}, {SelectedVarNames[1][1]}.");

		foreach(string[] Lines in DivisionsText)
        {
			if (UnityEngine.Random.Range(0, 2) == 0)
            {
				bool sound = true;
				//Each line in the division will have roughly a 1/3 chance of being problematic
				
				//If the division is supposed to be dealing with an int variable
				if (DivisionTypes[Array.IndexOf(DivisionsText, Lines)] == types.@int)
                {
					//Declaration alternate variable
					string VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0];
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][1];
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] = lineStatus.altVar;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} declared the alternate variable.");
						sound = false;
					}

					//Declaration misspelled keyword
					string TypeWord = "int";
					if(UnityEngine.Random.Range(0,10) == 0)
                    {
						TypeWord = KeywordsTypos[1][UnityEngine.Random.Range(0, KeywordsTypos[1].Length)];
						MisspelledKeywords++;
						Debug.Log($"Misspelled \"{TypeWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s declaration. There are now {MisspelledKeywords} misspelled keywords.");

						LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] = lineStatus.ignored;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s declaration should be IGNORED.");
						sound = false;
					}

					Lines[0] = $"{darkblue}{TypeWord}{end} {lightblue}{VariableWord}{end}";

					//Declaration semicolon problem
					if(UnityEngine.Random.Range(0,10) == 0)
                    {
						if(UnityEngine.Random.Range(0,2) == 0)
                        {
							Lines[0] += ";;";
							ExtraSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s declaration has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
							sound = false;
                        }
						else
                        {
							MissingSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s declaration has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
							sound = false;
						}
                    }
					else
                    {
						Lines[0] += ";";
                    }
					
					//Assignment alternate variable
					VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0];
					if (UnityEngine.Random.Range(0, 10) == 0)
                    {
						VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][1];
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][1] = lineStatus.altVar;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} assigned the alternate variable.");
						sound = false;
					}

					//Assignment type mismatch
					string ValueWord = $"{mint}{IntValues[UnityEngine.Random.Range(0, IntValues.Length)]}{end}";
					if (UnityEngine.Random.Range(0, 10) == 0)
                    {
						ValueWord = $"{brown}\"{StringValues[UnityEngine.Random.Range(0, StringValues.Length)]}\"{end}";
						sound = false;

						//The type mismatch only counts if the assignment VariableWord is the same as the declaration VariableWord
						if(LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] == LineStatuses[Array.IndexOf(DivisionsText, Lines)][1])
						{
							TypeMismatches++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment has a type mismatch. There are now {TypeMismatches} type mismatches.");
							LineStatuses[Array.IndexOf(DivisionsText, Lines)][1] = lineStatus.ignored;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment should be IGNORED.");
						}
					}

					Lines[1] = $"{lightblue}{VariableWord}{end} {grey}={end} {ValueWord}";

					//Assignment semicolon problem
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							Lines[1] += ";;";
							ExtraSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
							sound = false;
						}
						else
						{
							MissingSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
							sound = false;
						}
					}
					else
					{
						Lines[1] += ";";
					}

					//Read alternate variable
					VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0];
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][1];
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.altVar;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} read the alternate variable.");
						sound = false;
					}

					//Read misspelled keyword
					string SystemWord = "System";
					string ConsoleWord = "Console";
					string WriteLineWord = "WriteLine";
					if (UnityEngine.Random.Range(0, 29) == 0)
					{
						SystemWord = KeywordsTypos[3][UnityEngine.Random.Range(0, KeywordsTypos[3].Length)];
						MisspelledKeywords++;
						Debug.Log($"Misspelled \"{SystemWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s read. There are now {MisspelledKeywords} misspelled keywords.");
						
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
					}
					if (UnityEngine.Random.Range(0, 29) == 0)
					{
						ConsoleWord = KeywordsTypos[0][UnityEngine.Random.Range(0, KeywordsTypos[0].Length)];
						MisspelledKeywords++;
						Debug.Log($"Misspelled \"{ConsoleWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s read. There are now {MisspelledKeywords} misspelled keywords.");


						LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
					}
					if (UnityEngine.Random.Range(0, 29) == 0)
					{
						WriteLineWord = KeywordsTypos[4][UnityEngine.Random.Range(0, KeywordsTypos[4].Length)];
						MisspelledKeywords++;
						Debug.Log($"Misspelled \"{WriteLineWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s read. There are now {MisspelledKeywords} misspelled keywords.");


						LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
					}

					if (LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] == lineStatus.ignored)
                    {
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read should be IGNORED.");
						sound = false;
						Lines[2] = $"{SystemWord}{grey}.{end}{green}{ConsoleWord}{end}{grey}.{end}{yellow}{WriteLineWord}{end}({lightblue}{VariableWord}{end})";
					}
					else if(UnityEngine.Random.Range(0,9) < 4)
                    {
						Lines[2] = $"{SystemWord}{grey}.{end}{green}{ConsoleWord}{end}{grey}.{end}{yellow}{WriteLineWord}{end}({lightblue}{VariableWord}{end})";
					}
					else
                    {
						Lines[2] = $"{lightblue}{VariableWord}{end}{grey}++{end}";
					}

					//Read semicolon problem
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							Lines[2] += ";;";
							ExtraSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
							sound = false;
						}
						else
						{
							MissingSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
							sound = false;
						}
					}
					else
					{
						Lines[2] += ";";
					}
					if(sound)
					{
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} is sound.");
					}
				}
				//If the division is supposed to be dealing with a string variable
                else
                {
					sound = true;
					//Declaration alternate variable
					string VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0];
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][1];
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] = lineStatus.altVar;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} declared the alternate variable.");
						sound = false;
					}

					//Declaration misspelled keyword
					string TypeWord = "string";
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						TypeWord = KeywordsTypos[2][UnityEngine.Random.Range(0, KeywordsTypos[2].Length)];
						MisspelledKeywords++;
						Debug.Log($"Misspelled \"{TypeWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s declaration. There are now {MisspelledKeywords} misspelled keywords.");

						LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] = lineStatus.ignored;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s declaration should be IGNORED.");
						sound = false;
					}

					Lines[0] = $"{darkblue}{TypeWord}{end} {lightblue}{VariableWord}{end}";

					//Declaration semicolon problem
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							Lines[0] += ";;";
							ExtraSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s declaration has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
							sound = false;
						}
						else
						{
							MissingSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s declaration has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
							sound = false;
						}
					}
					else
					{
						Lines[0] += ";";
					}

					//Assignment alternate variable
					VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0];
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][1];
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][1] = lineStatus.altVar;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} assigned the alternate variable.");
						sound = false;
					}

					//Assignment type mismatch
					string ValueWord = $"{brown}\"{StringValues[UnityEngine.Random.Range(0, StringValues.Length)]}\"{end}";
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						ValueWord = $"{mint}{IntValues[UnityEngine.Random.Range(0, IntValues.Length)]}{end}";
						sound = false;

						//The type mismatch only counts if the assignment VariableWord is the same as the declaration VariableWord
						if(LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] == LineStatuses[Array.IndexOf(DivisionsText, Lines)][1])
						{
							TypeMismatches++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment has a type mismatch. There are now {TypeMismatches} type mismatches.");
							LineStatuses[Array.IndexOf(DivisionsText, Lines)][1] = lineStatus.ignored;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment should be IGNORED.");
						}
					}

					Lines[1] = $"{lightblue}{VariableWord}{end} {grey}={end} {ValueWord}";

					//Assignment semicolon problem
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							Lines[1] += ";;";
							ExtraSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
							sound = false;
						}
						else
						{
							MissingSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s assignment has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
							sound = false;
						}
					}
					else
					{
						Lines[1] += ";";
					}

					//Read alternate variable
					VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0];
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						VariableWord = SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][1];
						LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.altVar;
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} read the alternate variable.");
						sound = false;
					}

					//For a string read, a type mismatch and a misspelled keyword are both possible, but not at the same time
					//The probability of one of the two occurring is 10%
					switch(UnityEngine.Random.Range(0,20))
					{
						case 0:
						//Read type mismatch
						Lines[2] = $"{lightblue}{VariableWord}{end}{grey}++{end}";
						sound = false;

						//The type mismatch only counts if the read VariableWord is the same as the declaration VariableWord
						if(LineStatuses[Array.IndexOf(DivisionsText, Lines)][0] == LineStatuses[Array.IndexOf(DivisionsText, Lines)][2])
						{
							TypeMismatches++;
							LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read has a type mismatch.");
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read should be IGNORED.");
						}
						break;

						case 1:
						//Read misspelled keyword
						string SystemWord = "System";
						string ConsoleWord = "Console";
						string WriteLineWord = "WriteLine";
						
						//Force it to give at least one typo
						while(LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] != lineStatus.ignored)
						{
							if (UnityEngine.Random.Range(0, 29) == 0)
							{
								SystemWord = KeywordsTypos[3][UnityEngine.Random.Range(0, KeywordsTypos[3].Length)];
								MisspelledKeywords++;
								Debug.Log($"Misspelled \"{SystemWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s read. There are now {MisspelledKeywords} misspelled keywords.");


								LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
							}
							if (UnityEngine.Random.Range(0, 29) == 0)
							{
								ConsoleWord = KeywordsTypos[0][UnityEngine.Random.Range(0, KeywordsTypos[0].Length)];
								MisspelledKeywords++;
								Debug.Log($"Misspelled \"{ConsoleWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s read. There are now {MisspelledKeywords} misspelled keywords.");


								LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
							}
							if (UnityEngine.Random.Range(0, 29) == 0)
							{
								WriteLineWord = KeywordsTypos[4][UnityEngine.Random.Range(0, KeywordsTypos[4].Length)];
								MisspelledKeywords++;
								Debug.Log($"Misspelled \"{WriteLineWord}\" in division {Array.IndexOf(DivisionsText, Lines)}'s read. There are now {MisspelledKeywords} misspelled keywords.");

								LineStatuses[Array.IndexOf(DivisionsText, Lines)][2] = lineStatus.ignored;
							}
						}
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read should be IGNORED.");
						sound = false;
						Lines[2] = $"{SystemWord}{grey}.{end}{green}{ConsoleWord}{end}{grey}.{end}{yellow}{WriteLineWord}{end}({lightblue}{VariableWord}{end})";
						break;

						//Neither
						default:
						Lines[2] = $"System{grey}.{end}{green}Console{end}{grey}.{end}{yellow}WriteLine{end}({lightblue}{VariableWord}{end})";
						break;
					}

					//Read semicolon problem
					if (UnityEngine.Random.Range(0, 10) == 0)
					{
						if (UnityEngine.Random.Range(0, 2) == 0)
						{
							Lines[2] += ";;";
							ExtraSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
							sound = false;
						}
						else
						{
							MissingSemicolons++;
							Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)}'s read has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
							sound = false;
						}
					}
					else
					{
						Lines[2] += ";";
					}
					if(sound)
					{
						Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} is sound.");
					}
				}
            }
            else
            {
				//The division will have no problems
				Debug.Log($"Division {Array.IndexOf(DivisionsText, Lines)} is sound.");
				Lines[0] = $"{darkblue}{DivisionTypes[Array.IndexOf(DivisionsText, Lines)]}{end} {lightblue}{SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0]}{end};";

				if (DivisionTypes[Array.IndexOf(DivisionsText, Lines)] == types.@int)
				{
					Lines[1] = $"{lightblue}{SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0]}{end} {grey}={end} {mint}{IntValues[UnityEngine.Random.Range(0,IntValues.Length)]}{end};";
					if(UnityEngine.Random.Range(0,2) == 0)
                    {
						Lines[2] = $"{lightblue}{SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0]}{end}{grey}++{end};";
                    }
                    else
                    {
						Lines[2] = $"System{grey}.{end}{green}Console{end}{grey}.{end}{yellow}WriteLine{end}({lightblue}{SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0]}{end});";
                    }
				}
                else
                {
					Lines[1] = $"{lightblue}{SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0]}{end} {grey}={end} {brown}\"{StringValues[UnityEngine.Random.Range(0, StringValues.Length)]}\"{end};";
					Lines[2] = $"System{grey}.{end}{green}Console{end}{grey}.{end}{yellow}WriteLine{end}({lightblue}{SelectedVarNames[Array.IndexOf(DivisionsText, Lines)][0]}{end});";
				}
            }
        }
		//Write to the console if each line used the alternate variable or should be ignored
		Debug.Log($"{string.Join(", ",LineStatuses[0].Select(x => x.ToString()).ToArray())}; {string.Join(", ",LineStatuses[1].Select(x => x.ToString()).ToArray())}");

		foreach(lineStatus[] lines in LineStatuses)
		{
			//If the lineStatus of the declaration and assignment don't match and the assignment is not ignored, then the assignment must be trying to assign to an undeclared variable
			if(lines[0] != lines[1] && lines[1] != lineStatus.ignored)
			{
				AssignedBeforeDeclared = true;
				Debug.Log($"Division {Array.IndexOf(LineStatuses,lines)} tried to assign to an undeclared variable.");
			}

			//If not all three lineStatuses are the same and the read is not ignored, then the read must be trying to read an unassigned variable
			if(!(lines[0] == lines[1] && lines[1] == lines[2]) && lines[2] != lineStatus.ignored)
			{
				ReadBeforeAssigned = true;
				Debug.Log($"Division {Array.IndexOf(LineStatuses,lines)} tried to read an unassigned variable.");
				
				//Obviously, a variable being read before declared implies that it was read before assigned
				//I totally could have put this if statement separately but putting it inside its superset makes me feel cooler
				//If the lineStatus of the declaration and read don't match(and the read is not ignored, but that's handled by the outer if statement already), then the variable must have been read before it was declared
				if(lines[0] != lines[2])
				{
					ReadBeforeDeclared = true;
					Debug.Log($"Division {Array.IndexOf(LineStatuses,lines)} tried to read an undeclared variable.");
				}
			}
		}

		//Randomly interlace the divisions while keeping their individual orders
		List<string> ShuffledLines = new List<string>{"","","","","","",};
		List<int> Div1Indices = new List<int>{1,2,3,4,5};
		//Div0Indices is the indices for indices 1 and 2 of division 0
		List<int> Div0Indices = new List<int>();
		int temp;

		//Choose two random indices to move from Div1Indices to Div0Indices
		for(int i = 0; i < 2; i++)
		{
			temp = UnityEngine.Random.Range(0, Div1Indices.Count);
			Div0Indices.Add(Div1Indices[temp]);
			Div1Indices.RemoveAt(temp);
		}
		Div0Indices.Sort();

		//Line 0 of division 0 must always appear first
		ShuffledLines[0] = DivisionsText[0][0];
		
		//Put the lines from division 1 into ShuffledLines at the indices left in Div1Indices
		for(int i = 0; i < 3; i++)
		{
			ShuffledLines[Div1Indices[i]] = DivisionsText[1][i];
		}
		
		//Put the remaining lines from division 0 into ShuffledLines at the indices listed in Div0Indices
		ShuffledLines[Div0Indices[0]] = DivisionsText[0][1];
		ShuffledLines[Div0Indices[1]] = DivisionsText[0][2];

		//Logging uses Div1Indices and Div0Indices to work out which line went where in order to retrieve the lines it needs, and putting the 0 in here evens things out and helps with that
		//Because now index 0 of Div0Indices is the index of (line 0 division 0) in ShuffledLines
		Div0Indices.Insert(0, 0);

		//A final line(the "+=" line) that tries to deal with both divisions' variables gives one last opportunity for a type mismatch, semicolon problem or alternate variable
		//This line only has a 50% chance of appearing
		if(UnityEngine.Random.Range(0,2) == 0)
		{
			Debug.Log("The += line is present.");

			//+= line alternate variable
			string[] VariableWords = new string[]{SelectedVarNames[0][0],SelectedVarNames[1][0]};
			lineStatus[] Line7Statuses = new lineStatus[]{lineStatus.none, lineStatus.none};
			
			for(int i = 0; i < 2; i++)
			{
				if(UnityEngine.Random.Range(0,10) == 0)
				{
					VariableWords[i] = SelectedVarNames[i][1];
					Debug.Log($"The += line read the alternate variable {SelectedVarNames[i][1]}.");
					Line7Statuses[i] = lineStatus.altVar;
				}
			}

			//The two variables can appear in either order and whether or not it has a type mismatch is dependent on this order
			//Because stringVar += intVar; is valid but intVar += stringVar; is not
			bool ignored = false;
			if(UnityEngine.Random.Range(0,2) == 0)
			{
				ShuffledLines.Add($"{lightblue}{VariableWords[0]}{end} {grey}+={end} {lightblue}{VariableWords[1]}{end}");

				//if the division corresponding to the first VariableWord declared a type int and the second declared a type string AND the used variables were actually the ones declared
				if(DivisionTypes[0] == types.@int && DivisionTypes[1] == types.@string && Line7Statuses[0] == LineStatuses[0][0] && Line7Statuses[1] == LineStatuses[1][0])
				{
					TypeMismatches++;
					Debug.Log($"The += line has a type mismatch and should be IGNORED. There are now {TypeMismatches} type mismatches.");
					ignored = true;
				}
			}
			else
			{
				ShuffledLines.Add($"{lightblue}{VariableWords[1]}{end} {grey}+={end} {lightblue}{VariableWords[0]}{end}");

				//Same as above
				if(DivisionTypes[1] == types.@int && DivisionTypes[0] == types.@string && Line7Statuses[0] == LineStatuses[0][0] && Line7Statuses[1] == LineStatuses[1][0])
				{
					TypeMismatches++;
					Debug.Log($"The += line has a type mismatch and should be IGNORED. There are now {TypeMismatches} type mismatches.");
					ignored = true;
				}
			}

			//If line 7 is not ignored, calculate if it tried to read an undeclared or unassigned variable and set the variables
			if(!ignored)
			{
				for(int i = 0; i < 2; i++)
				{
					lineStatus CurrentVarStatus = lineStatus.none;
					if(VariableWords[i] == SelectedVarNames[i][1])
					{
						CurrentVarStatus = lineStatus.altVar;
					}
					//If the lineStatuses of the declaration, assignment and current VariableWord are not all the same, the current VariableWord can't have been assigned
					if(!((LineStatuses[i][0] == LineStatuses[i][1]) && (LineStatuses[i][1] == CurrentVarStatus)))
					{
						Debug.Log($"The += line tried to read unassigned variable {VariableWords[i]}.");
						ReadBeforeAssigned = true;

						//If the lineStatuses of the declaration and current VariableWord are not the same, the current VariableWord can't have been declared, and again this is a subset so I've nested it into its superset
						if(LineStatuses[i][0] != CurrentVarStatus)
						{
							Debug.Log($"The += line tried to read undeclared variable {VariableWords[i]}.");
							ReadBeforeDeclared = true;
						}
					}
				}
			}

			//+= line semicolon problem
			if (UnityEngine.Random.Range(0, 10) == 0)
			{
				if (UnityEngine.Random.Range(0, 2) == 0)
				{
					ShuffledLines[6] += ";;";
					ExtraSemicolons++;
					Debug.Log($"The += line has an extra semicolon. There are now {ExtraSemicolons} extra semicolons.");
				}
				else
				{
					MissingSemicolons++;
					Debug.Log($"The += line has a missing semicolon. There are now {MissingSemicolons} missing semicolons.");
				}
			}
			else
			{
				ShuffledLines[6] += ";";
			}
		}

		ShuffledLines.Add("}");

		//Simulate indentation
		for(int i = 0; i < ShuffledLines.Count; i++)
		{
			ShuffledLines[i] = "    " + ShuffledLines[i];
		}

		ShuffledLines.Add("}");
		
		//Finally, write the text to the screen
		ScreenText.text = $"{string.Join("\n", StartText)}\n{string.Join("\n", ShuffledLines.ToArray())}";

		//Normalise scaling of halos for different bomb sizes
		for(int i = 0; i < Halos.Length; i++)
		{
			Halos[i].range *= transform.lossyScale.x;
		}

		//Hide the ploded LEDs
		foreach(Renderer led in Plodedleds)
		{
			led.enabled = false;
		}

		Debug.Log($"Misspelled: {MisspelledKeywords} Missing: {MissingSemicolons} Extra: {ExtraSemicolons} TypeMismatches: {TypeMismatches}");

		//Shuffle the AllQuestionTypes array so that picking the first 10 items yields 10 random question types with no repeats
		MonoRandom rnd = RuleSeedable.GetRNG();
		rnd.ShuffleFisherYates(AllQuestionTypes);

		Question[] QuestionList = new Question[10];
		FlipCount = new int[]{0, 0, 0, 0};
		string[] LogArray = new string[10];
		for(int i = 0; i < 10; i++)
		{
			QuestionList[i] = MakeQuestion(AllQuestionTypes[i], rnd);
			if(QuestionList[i].IsTrue)
			{
				foreach(int num in QuestionList[i].FlippedLights)
				{
					FlipCount[num]++;
				}
			}

			//map positions [0, 1, 2, 3] to [1, 2, 3, 4] for logging
			for(int j = 0; j < QuestionList[i].FlippedLights.Count; j++)
			{
				QuestionList[i].FlippedLights[j]++;
			}
			Debug.Log($"{QuestionList[i].QuestionText} [{QuestionList[i].flipType}/s: {((QuestionList[i].flipType != FlipType.Position) ? ($"{string.Join(" ", QuestionList[i].ColourOrLabelFlips.Select(x => x.name).ToArray())}] [Equivalent Position/s: ") : (""))}{string.Join(" ", QuestionList[i].FlippedLights.Select(x => x.ToString()).ToArray())}] [{QuestionList[i].IsTrue}] [Accumulated flips: {string.Join(" ", FlipCount.Select(x => x.ToString()).ToArray())}]");
			LogArray[i] = $"{QuestionList[i].QuestionText} [{QuestionList[i].flipType}/s: {((QuestionList[i].flipType != FlipType.Position) ? ($"{string.Join(" ", QuestionList[i].ColourOrLabelFlips.Select(x => x.name).ToArray())}] [Equivalent Position/s: ") : (""))}{string.Join(" ", QuestionList[i].FlippedLights.Select(x => x.ToString()).ToArray())}] [{QuestionList[i].IsTrue}] [Accumulated flips: {string.Join(" ", FlipCount.Select(x => x.ToString()).ToArray())}]";
		}

		for(int i = 0; i < 4; i++)
		{
			FlipCount[i] %= 2;
		}
		Debug.Log(string.Join(" ", FlipCount.Select(x => x.ToString()).ToArray()));

		if(!FlipCount.Contains(1))
		{
			Debug.Log("Solution has no off lights! Regenerating puzzle..");
			Start();
		}
		
		Log($"The buttons were labelled: {string.Join(" ", Writings.Select(x => $"[{x.name}]").ToArray())}");
		Log($"The buttons were coloured: {string.Join(", ", ButtonColours.Select(x => x.name).ToArray())}");
		Log($"The screen said:\n{ScreenText.text}");
		Log($"The solution with ruleseed {RuleSeedable.GetRNG().Seed} was:\n{string.Join("\n",LogArray)}");
		Log($"The answer was: {string.Join(", ", FlipCount.Select(x => (x == 0 ? "on" : "off")).ToArray())}");


		Debug.Log(string.Join(", ", Div0Indices.Select(x => x.ToString()).ToArray()));
		Debug.Log(string.Join(", ", Div1Indices.Select(x => x.ToString()).ToArray()));

		Settings = new HelloWorldSettings{};

		if(ModSettings != null && !string.IsNullOrEmpty(ModSettings.Settings))
		{
			try
			{
				Settings = JsonConvert.DeserializeObject<HelloWorldSettings>(ModSettings.Settings);
			}
			catch(System.Exception e)
			{
				Debug.Log("JSON parsing failed: " + e.Message);
				Settings.SecondsBeforeCheck = 5;
				string defaultJson = JsonConvert.SerializeObject(Settings, Formatting.Indented);
				ModSettings.Settings = defaultJson;
			}
		}
		else
		{
			Settings.SecondsBeforeCheck = 5;
			if(ModSettings != null)
			{
				string defaultJson = JsonConvert.SerializeObject(Settings, Formatting.Indented);
				ModSettings.Settings = defaultJson;
			}
		}
		Debug.Log("The SecondsBeforeCheck is set to: " + Settings.SecondsBeforeCheck);
	}





















	void ButtonDown(KMSelectable button)
    {
		position = button.transform.localPosition;
		position.y = 0.026f;
		button.transform.localPosition = position;
		if (Leds[Array.IndexOf(Buttons, button)].enabled)
        {
			ButtonStates[Array.IndexOf(Buttons, button)]++;
			ButtonStates[Array.IndexOf(Buttons, button)] %= 3;
			PlodeCoroutines[Array.IndexOf(Buttons, button)] = StartCoroutine(xplodRoutine(Halos[Array.IndexOf(Buttons, button)], ButtonStates[Array.IndexOf(Buttons, button)]));
		}
	}
	
    void ButtonUp(KMSelectable button)
    {
		ButtonStates[Array.IndexOf(Buttons, button)]++;
		ButtonStates[Array.IndexOf(Buttons, button)] %= 3;
        position = button.transform.localPosition;
        position.y = 0.028f;
        button.transform.localPosition = position;
	}

	IEnumerator xplodRoutine(Light light, int startStateId)
	{
		int haloIndex = Array.IndexOf(Halos, light);

		float time = 0f;

		while (ButtonStates[haloIndex] == startStateId && light.range < 0.08f * transform.lossyScale.x)
		{

			if(Time.deltaTime == 0)
			{
				yield return null;
				continue;
			}

			//I never thought I'd be using calculus here of all places
			light.range += transform.lossyScale.x * 0.05f * (Mathf.Pow(1.8f,(10f * time) - 11f));

			light.intensity = light.range * 125f;
			yield return null;
			time += Time.deltaTime;
		}

		if(light.intensity > 10 * transform.lossyScale.x)

		{
			Log($"The LED in position {haloIndex + 1} was blown up.");
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CapacitorPop, transform);
			Leds[haloIndex].enabled = false;
			Halos[haloIndex].enabled = false;
			Plodedleds[haloIndex].enabled = true;

			if(CheckCoroutine != null)
			{
				StopCoroutine(CheckCoroutine);

			}
			CheckCoroutine = StartCoroutine(CheckRoutine());
			yield break;
		}

		while (ButtonStates[haloIndex] == (startStateId + 1) % 3 && light.range > 0.02f * transform.lossyScale.x
		)
		{
			light.range -= transform.lossyScale.x * Time.deltaTime / 2;
			light.intensity = light.range * 125f;
			yield return null;
		}
		light.range = 0.02f * transform.lossyScale.x;
		light.intensity = 1f;
		PlodeCoroutines[haloIndex] = null;
	}



	enum QuestionType
	{
		ColourLabelled,
		ColourNotLabelled,
		ColourLabelledorLabelled,
		ColourInPosOrPos,

		LabelInPos,
		LabelNotInPos,
		LabelIsColourorColour,
		LabelInPosOrPos,

		PosIsColour,
		PosNotColour,
		PosColourOrColour,
		PosLabelledorLabelled,

		TwoLabelsAdjacent,
		TwoColoursAdjacent,

		LabelAfterLabel,
		ColourAfterColour,

		OverMisspelled,
		UnderMisspelled,
		ExactlyMisspelled,

		OverMissing,
		UnderMissing,
		ExactlyMissing,

		OverExtra,
		UnderExtra,
		ExactlyExtra,

		OverTypes,
		UnderTypes,
		ExactlyTypes,

		AssignBeforeDeclare,
		ReadBeforeDeclare,
		ReadBeforeAssign
	}

	enum FlipType
	{
		Colour, Label, Position
	}

	class Question{
		public string QuestionText;
		public bool IsTrue;
		public List<int> FlippedLights;
		public FlipType flipType;
		public List<Material> ColourOrLabelFlips;
	}

	QuestionType[] AllQuestionTypes = (QuestionType[])Enum.GetValues(typeof(QuestionType));

	List<int> PositionsToFlip(MonoRandom rnd)
	{
		List<int> intList = new List<int>();
		while(intList.Count == 0)
		{
			for(int i = 0; i < 4; i++)
			{
				if(rnd.Next(2) == 1)
				{
					intList.Add(i);
				}
			}
		}
		return intList;
	}

	List<Material> ColoursToFlip(MonoRandom rnd)
	{
		List<Material> matList = new List<Material>();
		while(matList.Count == 0)
		{
			for(int i = 0; i < 4; i++)
			{
				if(rnd.Next(2) == 1)
				{
					matList.Add(ButtonMats[i]);
				}
			}
		}
		return matList;
	}

	List<Material> LabelsToFlip(MonoRandom rnd)
	{
		List<Material> matList = new List<Material>();
		while(matList.Count == 0)
		{
			for(int i = 0; i < 4; i++)
			{
				if(rnd.Next(2) == 1)
				{
					matList.Add(WritingMats[i]);
				}
			}
		}
		return matList;
	}

	Question MakeQuestion(QuestionType questiontype, MonoRandom rnd)
	{
		Material Colour1;
		Material Colour2;
		Material Label1;
		Material Label2;
		int Pos1;
		int Pos2;
		string Plural;
		FlipType ThisFlipType = FlipType.Colour;
		List<int> LightsToFlip;
		List<Material> ThisColourOrLabelFlips = new List<Material>();
		Question ReturnedQuestion;

		//A note: ButtonMats and WritingMats are never altered or rearranged, so the ruleseed picks colours and writing from those.
		//ButtonColours and Writings are the randomly generated colours and labels of the buttons from left to right, which is why I need to use Array.IndexOf.

		switch(questiontype)
		{
			case QuestionType.ColourLabelled:
				Colour1 = ButtonMats[rnd.Next(4)];
				Label1 = WritingMats[rnd.Next(4)];
				
				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The {Colour1.name} button is labelled \"{Label1.name}\".",
					IsTrue = (Array.IndexOf(ButtonColours, Colour1) == Array.IndexOf(Writings, Label1)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.ColourNotLabelled:
				Colour1 = ButtonMats[rnd.Next(4)];
				Label1 = WritingMats[rnd.Next(4)];

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The {Colour1.name} button is not labelled \"{Label1.name}\".",
					IsTrue = (Array.IndexOf(ButtonColours, Colour1) != Array.IndexOf(Writings, Label1)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.ColourLabelledorLabelled:
				Colour1 = ButtonMats[rnd.Next(4)];
				Label1 = WritingMats[rnd.Next(4)];
				Label2 = WritingMats[rnd.Next(4)];
				while(Label1 == Label2)
				{
					Label2 = WritingMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The {Colour1.name} button is labelled \"{Label1.name}\" or \"{Label2.name}\".",
					IsTrue = ((Array.IndexOf(ButtonColours, Colour1) == Array.IndexOf(Writings, Label1)) || (Array.IndexOf(ButtonColours, Colour1) == Array.IndexOf(Writings, Label2))),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.ColourInPosOrPos:
				Colour1 = ButtonMats[rnd.Next(4)];
				Pos1 = rnd.Next(4);
				Pos2 = rnd.Next(4);
				while(Pos1 == Pos2)
				{
					Pos2 = rnd.Next(4);
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The {Colour1.name} button is in position {Pos1 + 1} or {Pos2 + 1}.",
					IsTrue = ((Array.IndexOf(ButtonColours, Colour1) == Pos1) || (Array.IndexOf(ButtonColours, Colour1) == Pos2)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.LabelInPos:
				Label1 = WritingMats[rnd.Next(4)];
				Pos1 = rnd.Next(4);

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The \"{Label1.name}\" button is in position {Pos1 + 1}.",
					IsTrue = (Array.IndexOf(Writings, Label1) == Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.LabelNotInPos:
				Label1 = WritingMats[rnd.Next(4)];
				Pos1 = rnd.Next(4);

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The \"{Label1.name}\" button is not in position {Pos1 + 1}.",
					IsTrue = (Array.IndexOf(Writings, Label1) != Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.LabelIsColourorColour:
				Label1 = WritingMats[rnd.Next(4)];
				Colour1 = ButtonMats[rnd.Next(4)];
				Colour2 = ButtonMats[rnd.Next(4)];
				while(Colour1 == Colour2)
				{
					Colour2 = ButtonMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The \"{Label1.name}\" button is {Colour1.name} or {Colour2.name}.",
					IsTrue = ((Array.IndexOf(Writings, Label1) == Array.IndexOf(ButtonColours, Colour1)) || (Array.IndexOf(Writings, Label1) == Array.IndexOf(ButtonColours, Colour2))),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;


			case QuestionType.LabelInPosOrPos:
				Label1 = WritingMats[rnd.Next(4)];
				Pos1 = rnd.Next(4);
				Pos2 = rnd.Next(4);
				while(Pos1 == Pos2)
				{
					Pos2 = rnd.Next(4);
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The \"{Label1.name}\" button is in position {Pos1 + 1} or {Pos2 + 1}.",
					IsTrue = ((Array.IndexOf(Writings, Label1) == Pos1) || (Array.IndexOf(Writings, Label1) == Pos2)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
					};
				return ReturnedQuestion;



			case QuestionType.PosIsColour:
				Pos1 = rnd.Next(4);
				Colour1 = ButtonMats[rnd.Next(4)];

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The button in position {Pos1 + 1} is {Colour1.name}.",
					IsTrue = (ButtonColours[Pos1] == Colour1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.PosNotColour:
				Pos1 = rnd.Next(4);
				Colour1 = ButtonMats[rnd.Next(4)];

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The button in position {Pos1 + 1} is not {Colour1.name}.",
					IsTrue = (ButtonColours[Pos1] != Colour1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.PosColourOrColour:
				Pos1 = rnd.Next(4);
				Colour1 = ButtonMats[rnd.Next(4)];
				Colour2 = ButtonMats[rnd.Next(4)];
				while(Colour1 == Colour2)
				{
					Colour2 = ButtonMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The button in position {Pos1 + 1} is {Colour1.name} or {Colour2.name}.",
					IsTrue = ((ButtonColours[Pos1] == Colour1) || (ButtonColours[Pos1] == Colour2)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.PosLabelledorLabelled:
				Pos1 = rnd.Next(4);
				Label1 = WritingMats[rnd.Next(4)];
				Label2 = WritingMats[rnd.Next(4)];
				while(Label1 == Label2)
				{
					Label2 = WritingMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The button in position {Pos1 + 1} says \"{Label1.name}\" or \"{Label1.name}\".",
					IsTrue = ((Writings[Pos1] == Label1) || (Writings[Pos1] == Label2)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.TwoLabelsAdjacent:
				Label1 = WritingMats[rnd.Next(4)];
				Label2 = WritingMats[rnd.Next(4)];
				while(Label1 == Label2)
				{
					Label2 = WritingMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The buttons labelled \"{Label1.name}\" and \"{Label2.name}\" are directly next to each other.",
					IsTrue = (Math.Abs(Array.IndexOf(Writings, Label1) - Array.IndexOf(Writings, Label2)) == 1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.TwoColoursAdjacent:
				Colour1 = ButtonMats[rnd.Next(4)];
				Colour2 = ButtonMats[rnd.Next(4)];
				while(Colour1 == Colour2)
				{
					Colour2 = ButtonMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The {Colour1.name} and {Colour2.name} buttons are directly next to each other.",
					IsTrue = (Math.Abs(Array.IndexOf(ButtonColours, Colour1) - Array.IndexOf(ButtonColours, Colour2)) == 1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.LabelAfterLabel:
				Label1 = WritingMats[rnd.Next(4)];
				Label2 = WritingMats[rnd.Next(4)];
				while(Label1 == Label2)
				{
					Label2 = WritingMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The \"{Label1.name}\" button appears to the left of the \"{Label2.name}\" button.",
					IsTrue = (Array.IndexOf(Writings, Label1) < Array.IndexOf(Writings, Label2)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.ColourAfterColour:
				Colour1 = ButtonMats[rnd.Next(4)];
				Colour2 = ButtonMats[rnd.Next(4)];
				while(Colour1 == Colour2)
				{
					Colour2 = ButtonMats[rnd.Next(4)];
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = $"The {Colour1.name} button appears to the left of the {Colour2.name} button.",
					IsTrue = (Array.IndexOf(ButtonColours, Colour1) < Array.IndexOf(ButtonColours, Colour2)),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.OverMisspelled:
				//I'm not using Pos1 as a position here, I just dont wanna create another int variable LOL
				Pos1 = rnd.Next(1,4);

				if(Pos1 == 1)
				{
					Plural = "There is at least 1 misspelled keyword.";
				}
				else
				{
					Plural = $"There are at least {Pos1} misspelled keywords.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (MisspelledKeywords >= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.UnderMisspelled:
				Pos1 = rnd.Next(4);

				if(Pos1 == 0)
				{
					Plural = "There are no misspelled keywords.";
				}
				else if (Pos1 == 1)
				{
					Plural = "There is no more than 1 misspelled keyword.";
				}
				else
				{
					Plural = $"There are no more than {Pos1} misspelled keywords.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (MisspelledKeywords <= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.ExactlyMisspelled:
				Pos1 = rnd.Next(1,4);

				if(Pos1 == 1)
				{
					Plural = "There is exactly 1 misspelled keyword.";
				}
				else
				{
					Plural = $"There are exactly {Pos1} misspelled keywords.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (MisspelledKeywords == Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.OverMissing:
				Pos1 = rnd.Next(1,3);
				if(Pos1 == 1)
				{
					Plural = "There is at least 1 missing semicolon.";
				}
				else
				{
					Plural = "There are at least 2 missing semicolons.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (MissingSemicolons >= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.UnderMissing:
				Pos1 = rnd.Next(0,3);

				if(Pos1 == 0)
				{
					Plural = "There are no missing semicolons.";
				}
				else if (Pos1 == 1)
				{
					Plural = "There is no more than 1 missing semicolon.";
				}
				else
				{
					Plural = "There are no more than 2 missing semicolons.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (MissingSemicolons <= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.ExactlyMissing:
				Pos1 = rnd.Next(1,4);

				if(Pos1 == 1)
				{
					Plural = "There is exactly 1 missing semicolon.";
				}
				else
				{
					Plural = $"There are exactly {Pos1} missing semicolons.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (MissingSemicolons == Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.OverExtra:
				Pos1 = rnd.Next(1,3);
				if(Pos1 == 1)
				{
					Plural = "There is at least 1 extra semicolon.";
				}
				else
				{
					Plural = "There are at least 2 extra semicolons.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (ExtraSemicolons >= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.UnderExtra:
				Pos1 = rnd.Next(0,3);

				if(Pos1 == 0)
				{
					Plural = "There are no extra semicolons.";
				}
				else if (Pos1 == 1)
				{
					Plural = "There is no more than 1 extra semicolon.";
				}
				else
				{
					Plural = "There are no more than 2 extra semicolons.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (ExtraSemicolons <= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.ExactlyExtra:
				Pos1 = rnd.Next(1,4);

				if(Pos1 == 1)
				{
					Plural = "There is exactly 1 extra semicolon.";
				}
				else
				{
					Plural = $"There are exactly {Pos1} extra semicolons.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (ExtraSemicolons == Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.OverTypes:
				Pos1 = rnd.Next(1,3);
				if(Pos1 == 1)
				{
					Plural = "There is at least 1 type mismatch.";
				}
				else
				{
					Plural = "There are at least 2 type mismatches.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (TypeMismatches >= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.UnderTypes:
				Pos1 = rnd.Next(0,3);

				if(Pos1 == 0)
				{
					Plural = "There are no type mismatches.";
				}
				else if (Pos1 == 1)
				{
					Plural = "There is no more than 1 type mismatch.";
				}
				else
				{
					Plural = "There are no more than 2 type mismatches.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (TypeMismatches <= Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;


			case QuestionType.ExactlyTypes:
				Pos1 = rnd.Next(1,3);

				if(Pos1 == 1)
				{
					Plural = "There is exactly 1 type mismatch.";
				}
				else
				{
					Plural = $"There are exactly {Pos1} type mismatches.";
				}

				LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
					QuestionText = Plural,
					IsTrue = (TypeMismatches == Pos1),
					FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
				};
				return ReturnedQuestion;



			case QuestionType.AssignBeforeDeclare:
			LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
				QuestionText = "The program tried to assign to a variable before it was declared.",
				IsTrue = AssignedBeforeDeclared,
				FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
			};
			return ReturnedQuestion;


			case QuestionType.ReadBeforeDeclare:
			LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
				QuestionText = "The program tried to read a variable before it was declared.",
				IsTrue = ReadBeforeDeclared,
				FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
			};
			return ReturnedQuestion;


			case QuestionType.ReadBeforeAssign:
			LightsToFlip = new List<int>();
				switch(rnd.Next(0,3))
				{
					case 0:
						ThisFlipType = FlipType.Position;
						LightsToFlip = PositionsToFlip(rnd);
						ThisColourOrLabelFlips = new List<Material>();
						break;

					case 1:
						ThisFlipType = FlipType.Colour;
						ThisColourOrLabelFlips = ColoursToFlip(rnd);
						foreach(Material Colour in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(ButtonColours,Colour));
						}
						break;

					case 2:
						ThisFlipType = FlipType.Label;
						ThisColourOrLabelFlips = LabelsToFlip(rnd);
						foreach(Material Label in ThisColourOrLabelFlips)
						{
							LightsToFlip.Add(Array.IndexOf(Writings,Label));
						}
						break;
				}
				ReturnedQuestion = new Question{
				QuestionText = "The program tried to read a variable before it was assigned to.",
				IsTrue = ReadBeforeAssigned,
				FlippedLights = LightsToFlip,
					flipType = ThisFlipType,
					ColourOrLabelFlips = ThisColourOrLabelFlips
			};
			return ReturnedQuestion;

			default:
			return new Question{};
		}
	}
	public class HelloWorldSettings
	{
		public int SecondsBeforeCheck = 5;
	}

	IEnumerator CheckRoutine()
	{
		float time = 0f;
		while(time < Settings.SecondsBeforeCheck)
		{
			time += Time.deltaTime;
			yield return null;
		}
		bool CorrectNow = true;
		for(int i = 0; i < 4; i++)
		{
			if(((FlipCount[i] == 1) && (Leds[i].enabled == true)) || ((FlipCount[i] == 0) && (Leds[i].enabled == false)))
			{
				CorrectNow = false;
			}
		}
		if(CorrectNow)
		{
			Log("Submitted correct answer. Solved.");
			Module.HandlePass();
		}
		else
		{
			Log($"Incorrectly submitted {string.Join(", ", Leds.Select(x => (x.enabled ? "on" : "off")).ToArray())}. Striking, then solving.");
			Module.HandleStrike();
			yield return new WaitForSeconds(0.7f);
			Module.HandlePass();
		}
		CheckCoroutine = null;
	}

	private readonly string TwitchHelpMessage = @"Hello, World!: !{0} G B Y [Press the green, blue and yellow buttons] | !{0} 1 4 [Press the 1st and 4th buttons] | !{0} Colorblind [Toggle colorblind mode]";
	string[] ColourLetters = new string[]{"r", "y", "g", "b"};


	//UNFINISHED: trying to blow up a single button doesn't work because there are no spaces, and
	//buttons don't release after being pressed yet
	IEnumerator ProcessTwitchCommand(string command)
	{
		List<KMSelectable> HeldButtons = new List<KMSelectable>();
		command = command.ToLowerInvariant();
		if(command.Contains("colorblind") || command.Contains("colourblind"))
		{
			yield return new KMSelectable[0];
			foreach(Renderer Label in ColourblindLabels)
			{
				Label.enabled = !Label.enabled;
			}
		}
		else
		{
			for(int i = 0; i < 4; i++)
			{
				if((command.Contains(" " + ColourLetters[i]) || command.Contains(ColourLetters[i] + " ")) || (command.Length == 1 && command.Contains(ColourLetters[i])))
				{
					HeldButtons.Add(Buttons[Array.IndexOf(ButtonColours, ButtonMats[i])]);
					yield return null;
					yield return Buttons[Array.IndexOf(ButtonColours, ButtonMats[i])];
					while(Leds[Array.IndexOf(ButtonColours, ButtonMats[i])].enabled)
					{
						yield return null;
					}
					yield return Buttons[Array.IndexOf(ButtonColours, ButtonMats[i])];
				}
				if((command.Contains(" " + (i + 1)) || command.Contains((i + 1) + " ") || ((command.Length == 1 && command.Contains((i + 1).ToString())))) && !HeldButtons.Contains(Buttons[i]))
				{
					HeldButtons.Add(Buttons[i]);
					yield return null;
					yield return Buttons[i];
					while(Leds[i].enabled)
					{
						yield return null;
					}
					yield return Buttons[i];
				}
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		for(int i = 0; i < 4; i++)
		{
			if (FlipCount[i] == 1)
			{
				yield return null;
				Buttons[i].OnInteract();
				while(Leds[i].enabled)
				{
					yield return null;
				}
				Buttons[i].OnInteractEnded();
			}
			
		}
	}
	void Log(string comment)
	{
		Debug.Log($"[Hello, World! #{moduleId}] {comment}");
	}
}
