using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using UnityEngine;
using KModkit;

public class hypercolorScript : MonoBehaviour {
	class Vertex
	{
		public bool X;
		public bool Y;
		public bool Z;
		public bool W;
		public Color VertexColor;

		public Vertex(bool X, bool Y, bool Z, bool W)
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
			this.W = W;
		}
	}
	
	class Hypercube
	{
		public Vertex[] vert = new Vertex[]
		{
			new Vertex(false, true, true, true),
			new Vertex(false, false, true, true),
			
			new Vertex(true, true, true, true),
			new Vertex(true, false, true, true),
			
			new Vertex(false, true, true, false),
			new Vertex(false, false, true, false),
			
			new Vertex(true, true, true, false),
			new Vertex(true, false, true, false),
			
			new Vertex(false, true, false, true),
			new Vertex(false, false, false, true),
			
			new Vertex(true, true, false, true),
			new Vertex(true, false, false, true),
			
			new Vertex(false, true, false, false),
			new Vertex(false, false, false, false),
			
			new Vertex(true, true, false, false),
			new Vertex(true, false, false, false)
		};

		public int FindVertex(bool X, bool Y, bool Z, bool W)
		{
			for (int i = 0; i < 16; i++)
			{
				if (this.vert[i].X == X && this.vert[i].Y == Y && this.vert[i].Z == Z &&
				    this.vert[i].W == W)
				{
					return i;
				}
			}

			return -1;
		}

		bool GetSign(string rot, string rem, string sign, int rsig, char c)
		{
			if (rot.Contains(c))
			{
				return sign[rot.IndexOf(c)] == '+' ? true : false;
			}
			else
			{
				if (rem[0] == c)
				{
					return rsig / 2 == 1 ? true : false;
				}
				else
				{
					return rsig % 2 == 1 ? true : false;
				}
			}
		}
		public int GetVertex(string rot, string sign, string rem, int rsig)
		{
			bool[] temp = new bool[4];
			temp[0] = GetSign(rot, rem, sign, rsig, 'X');
			temp[1] = GetSign(rot, rem, sign, rsig, 'Y');
			temp[2] = GetSign(rot, rem, sign, rsig, 'Z');
			temp[3] = GetSign(rot, rem, sign, rsig, 'W');
			for (int i = 0; i < 16; i++)
			{
				if (this.vert[i].X == temp[0] && this.vert[i].Y == temp[1] && this.vert[i].Z == temp[2] &&
				    this.vert[i].W == temp[3])
				{
					return i;
				}
			}
			return -1;
		}
	}
	
	public KMAudio bombAudio;
	public KMBombInfo bomb;
	public GameObject module;
	public GameObject[] vertices;
	public GameObject background;

	private Color[] PrimColors = new Color[8]
	{
		new Color(0F, 0F, 0F),
		new Color(0F, 0F, 0.5F),
		new Color(0F, 0.5F, 0F),
		new Color(0F, 0.5F, 0.5F),
		new Color(0.5F, 0F, 0F),
		new Color(0.5F, 0F, 0.5F),
		new Color(0.5F, 0.5F, 0F),
		new Color(0.5F, 0.5F, 0.5F)
	};
	private Hypercube c1 = new Hypercube();
	private Hypercube c2 = new Hypercube();
	private int targetVertex;
	private int targetColor;

	private List<string> Rotations = new List<string>()
	{
		"XY", "XZ", "XW", "YX", "YZ", "YW", "ZX", "ZY", "ZW", "WX", "WY", "WZ"
	};

	private bool animationPlaying = false;
	private bool moduleSolved = false;

	//logging
	static int moduleIdCounter = 1;
	int moduleId;
	
	public string TwitchHelpMessage = "Use !{0} top-right-back-zag to press that vertex. Use !{0} b or !{0} bg to press the background.";
	
	void Awake(){
		moduleId = moduleIdCounter++;
		
		background.GetComponent<KMSelectable>().OnInteract += delegate () { backgroundPress(); return false; };
		foreach (GameObject vertex in vertices){
			KMSelectable pressedvertex = vertex.GetComponent<KMSelectable>();
			for (int i=0;i<vertices.Length;i++){
				if (pressedvertex == vertices[i].GetComponent<KMSelectable>()){
					pressedvertex.OnInteract += delegate () { VertexPress(pressedvertex, i); return false; };
					break;}}
		}
	}
	void Start ()
	{
		defineColors();
		displayVertexsColor();
	}

	void defineColors()
	{
		List<Color> VertiColor = new List<Color>();
		targetColor = UnityEngine.Random.Range(0, PrimColors.Length);
		VertiColor.Add(PrimColors[targetColor]);
		foreach (var remaincolor in PrimColors)
		{
			if (remaincolor != PrimColors[targetColor])
			{
				VertiColor.Add(remaincolor);
				VertiColor.Add(remaincolor);
			}
		}
		while (!(VertiColor.Count >= 32))
		{
			var tempColor = UnityEngine.Random.Range(0, PrimColors.Length);
			while (tempColor == targetColor)
			{
				tempColor = UnityEngine.Random.Range(0, PrimColors.Length);
			}
			VertiColor.Add(PrimColors[tempColor]);
		}
		VertiColor.Shuffle();
		for (int i = 0; i < 16; i++)
		{
			c1.vert[i].VertexColor = VertiColor[i];
			c2.vert[i].VertexColor = VertiColor[i + 16];
		}
		Debug.LogFormat("[The Hypercolor #{0}] Your target color is {1}", moduleId, GetColor(PrimColors[targetColor]));
		Debug.LogFormat("[The Hypercolor #{0}] Cube 1 is", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]   {1}/{2} ----- {3}/{4}", moduleId
			, GetColor(c1.vert[0].VertexColor), GetColor(c1.vert[1].VertexColor)
			, GetColor(c1.vert[2].VertexColor), GetColor(c1.vert[3].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}]   /|        /|", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]  / |       / |", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]{1}/{2} +---- {3}/{4} |", moduleId
			, GetColor(c1.vert[4].VertexColor), GetColor(c1.vert[5].VertexColor)
			, GetColor(c1.vert[6].VertexColor), GetColor(c1.vert[7].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}] |  |      |  |", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}] | {1}/{2} ----+ {3}/{4}", moduleId
			, GetColor(c1.vert[8].VertexColor), GetColor(c1.vert[9].VertexColor)
			, GetColor(c1.vert[10].VertexColor), GetColor(c1.vert[11].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}] | /       | /", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}] |/        |/", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]{1}/{2} ----- {3}/{4}", moduleId
			, GetColor(c1.vert[12].VertexColor), GetColor(c1.vert[13].VertexColor)
			, GetColor(c1.vert[14].VertexColor), GetColor(c1.vert[15].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}] Cube 2 is", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]   {1}/{2} ----- {3}/{4}", moduleId
			, GetColor(c2.vert[0].VertexColor), GetColor(c2.vert[1].VertexColor)
			, GetColor(c2.vert[2].VertexColor), GetColor(c2.vert[3].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}]   /|        /|", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]  / |       / |", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]{1}/{2} +---- {3}/{4} |", moduleId
			, GetColor(c2.vert[4].VertexColor), GetColor(c2.vert[5].VertexColor)
			, GetColor(c2.vert[6].VertexColor), GetColor(c2.vert[7].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}] |  |      |  |", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}] | {1}/{2} ----+ {3}/{4}", moduleId
			, GetColor(c2.vert[8].VertexColor), GetColor(c2.vert[9].VertexColor)
			, GetColor(c2.vert[10].VertexColor), GetColor(c2.vert[11].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}] | /       | /", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}] |/        |/", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]{1}/{2} ----- {3}/{4}", moduleId
			, GetColor(c2.vert[12].VertexColor), GetColor(c2.vert[13].VertexColor)
			, GetColor(c2.vert[14].VertexColor), GetColor(c2.vert[15].VertexColor));
	}

	string GetColor(Color c)
	{
		if (c == new Color(0F, 0F, 0F))
		{
			return "K";
		}
		else if (c == new Color(0F, 0F, 0.5F))
		{
			return "B";
		}
		else if (c == new Color(0F, 0.5F, 0F))
		{
			return "G";
		}
		else if (c == new Color(0F, 0.5F, 0.5F))
		{
			return "C";
		}
		else if (c == new Color(0.5F, 0F, 0F))
		{
			return "R";
		}
		else if (c == new Color(0.5F, 0F, 0.5F))
		{
			return "M";
		}
		else if (c == new Color(0.5F, 0.5F, 0F))
		{
			return "Y";
		}
		else if (c == new Color(0.5F, 0.5F, 0.5F))
		{
			return "W";
		}

		return "";
	}
	void displayVertexsColor()
	{
		StartCoroutine(ColorTransition(false));
		for (int i = 0; i < 16; i++)
		{
			if (c1.vert[i].VertexColor == PrimColors[targetColor] || c2.vert[i].VertexColor == PrimColors[targetColor])
			{
				targetVertex = i;
				Debug.LogFormat("[The Hypercolor #{0}] Target vertex is now {1}", moduleId, GetVertexCoords(i));
			}
		}
	}
	
	IEnumerator ColorTransition(bool havetorestart)
	{
		animationPlaying = true;
		Color[] ColorDifs = new Color[16];
		for (int i = 0; i < 16; i++)
		{
			ColorDifs[i] = (new Color(c1.vert[i].VertexColor.r + c2.vert[i].VertexColor.r,
				                c1.vert[i].VertexColor.g + c2.vert[i].VertexColor.g,
				                c1.vert[i].VertexColor.b + c2.vert[i].VertexColor.b)
			                - vertices[i].GetComponent<MeshRenderer>().material.color)/75;
		}
		for (int i = 0; i < 75; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				vertices[j].GetComponent<MeshRenderer>().material.color += ColorDifs[j];
			}
			yield return new WaitForSeconds(0.01f);
		}

		if (havetorestart)
		{
			yield return new WaitForSeconds(1f);
		}

		animationPlaying = false;
		yield return null;
		if (havetorestart)
		{
			Start();
		}
	}

	string GetVertexCoords(int ind)
	{
		string f = "";
		f += (c1.vert[ind].X ? "right" : "left") + "-" + (c1.vert[ind].Y ? "top" : "bottom") + "-" + (c1.vert[ind].Z ? "back" : "front") + "-" + (c1.vert[ind].W ? "zag" : "zig");
		return f;
	}
	void RotateCube(Hypercube c, string rot)
	{
		for (int i = 0; i < 4; i++)
		{
			string remainder = "XYZW";
			remainder = remainder.Replace(rot[0].ToString(), string.Empty);
			remainder = remainder.Replace(rot[1].ToString(), string.Empty);
			Color temp = c.vert[c.GetVertex(rot, "--", remainder, i)].VertexColor;
			c.vert[c.GetVertex(rot, "--", remainder, i)].VertexColor =
				c.vert[c.GetVertex(rot, "-+", remainder, i)].VertexColor;
			c.vert[c.GetVertex(rot, "-+", remainder, i)].VertexColor =
				c.vert[c.GetVertex(rot, "++", remainder, i)].VertexColor;
			c.vert[c.GetVertex(rot, "++", remainder, i)].VertexColor =
				c.vert[c.GetVertex(rot, "+-", remainder, i)].VertexColor;
			c.vert[c.GetVertex(rot, "+-", remainder, i)].VertexColor = temp;
		}
	}
	void backgroundPress()
	{
		if(moduleSolved||animationPlaying){
			return;
		}
		
		background.GetComponent<KMSelectable>().AddInteractionPunch(0.25F);
		bombAudio.PlaySoundAtTransform("rotatesfx",transform);
		Rotations.Shuffle();
		RotateCube(c1, Rotations[0]);
		RotateCube(c2, Rotations[1]);
		Debug.LogFormat("[The Hypercolor #{0}] Background Pressed, Rotating each cube with {1} / {2}", moduleId, Rotations[0], Rotations[1]);
		displayVertexsColor();
	}
	void VertexPress(KMSelectable vertex, int index)
	{
		if(moduleSolved||animationPlaying){
			return;
		}
		
		vertex.AddInteractionPunch(0.1F);

		if (targetVertex == index)
		{
			StartCoroutine(Solve());
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[The Hypercolor #{0}] You have to press {1}, You pressed {2}. Strike!", moduleId, GetVertexCoords(targetVertex), GetVertexCoords(index));
			for (int i = 0; i < 16; i++)
			{
				c1.vert[i].VertexColor = Color.black;
				c2.vert[i].VertexColor = Color.black;
			}
			StartCoroutine(ColorTransition(true));
		}
	}
	
	IEnumerator Solve()
	{
		for (int i = 0; i < 16; i++)
		{
			c1.vert[i].VertexColor = Color.black;
			c2.vert[i].VertexColor = Color.black;
		}
		StartCoroutine(ColorTransition(false));
		bombAudio.PlaySoundAtTransform("solvesfx", transform);
		GetComponent<KMBombModule>().HandlePass();
		moduleSolved = true;
		yield return null;
	}
	
	public IEnumerator ProcessTwitchCommand(string command){

		string[] cutInBlank = command.Split(new char[] {' '});

		if (cutInBlank.Length == 1)
		{
			if (cutInBlank[0].Equals("B", StringComparison.InvariantCultureIgnoreCase) 
			    || cutInBlank[0].Equals("BG", StringComparison.InvariantCultureIgnoreCase))
			{
				background.GetComponent<KMSelectable>().OnInteract();
				yield return null;
			}
			else
			{
				bool valid = true;
				string[] axes = cutInBlank[0].Split(new char[] {'-'});
				if (axes.Length != 4)
				{
					valid = false;
				}
				int[] states = new int[4]{0, 0, 0, 0};
				foreach (var axe in axes)
				{
					if (axe.Equals("top", StringComparison.InvariantCultureIgnoreCase))
					{
						states[1] = 1;
					}
					if (axe.Equals("bottom", StringComparison.InvariantCultureIgnoreCase))
					{
						states[1] = -1;
					}
					if (axe.Equals("left", StringComparison.InvariantCultureIgnoreCase))
					{
						states[0] = -1;
					}
					if (axe.Equals("right", StringComparison.InvariantCultureIgnoreCase))
					{
						states[0] = 1;
					}
					if (axe.Equals("back", StringComparison.InvariantCultureIgnoreCase))
					{
						states[2] = 1;
					}
					if (axe.Equals("front", StringComparison.InvariantCultureIgnoreCase))
					{
						states[2] = -1;
					}
					if (axe.Equals("zag", StringComparison.InvariantCultureIgnoreCase))
					{
						states[3] = 1;
					}
					if (axe.Equals("zig", StringComparison.InvariantCultureIgnoreCase))
					{
						states[3] = -1;
					}
				}

				foreach (var state in states)
				{
					if (state == 0)
					{
						valid = false;
					}
				}

				if (valid)
				{
					vertices[c1.FindVertex(states[0] == 1, states[1] == 1, states[2] == 1, states[3] == 1)]
						.GetComponent<KMSelectable>().OnInteract();
					yield return null;
				}
				
			}
		}
	}
}
