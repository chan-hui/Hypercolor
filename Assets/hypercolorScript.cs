using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class hypercolorScript : MonoBehaviour
{
	class Vertex
	{
		public bool X = false;
		public bool Y = false;
		public bool Z = false;
		public bool W = false;
		public Color VertexColor;
		
		public Vertex(bool iX, bool iY, bool iZ, bool iW)
		{
			X = iX;
			Y = iY;
			Z = iZ;
			W = iW;
		}

		public Vertex(Vertex pv)
		{
			X = pv.X;
			Y = pv.Y;
			Z = pv.Z;
			W = pv.W;
			VertexColor = pv.VertexColor;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(Vertex)) return false;
			var other = (Vertex) obj;
			return VertexColor == other.VertexColor;
		}
		
		public static bool operator ==(Vertex v, Vertex v2)
		{
			return Equals(v, v2);
		}
		
		public static bool operator !=(Vertex v, Vertex v2)
		{
			return !Equals(v, v2);
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

		public Hypercube()
		{
		}
		public Hypercube(Hypercube previousCube)
		{
			for (int i = 0; i < 16; i++)
			{
				this.vert[i] = new Vertex(previousCube.vert[i]);
			}
		}
		
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(Hypercube)) return false;
			var other = (Hypercube) obj;
			return Enumerable.SequenceEqual(vert, other.vert);
		}
		public override int GetHashCode()
		{
			return vert.GetHashCode();
		}
		public static bool operator ==(Hypercube v, Hypercube v2)
		{
			return Equals(v, v2);
		}
		public static bool operator !=(Hypercube v, Hypercube v2)
		{
			return !Equals(v, v2);
		}
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
	public KMSelectable background;

	private Color[] PrimColorsR = new Color[]
	{
		new Color(0F, 0F, 0F),
		new Color(0.5F, 0F, 0F),
		new Color(1F, 0F, 0F)
	};

	private Color[] PrimColorsG = new Color[]
	{
		new Color(0F, 0F, 0F),
		new Color(0F, 0.5F, 0F),
		new Color(0F, 1F, 0F)
	};

	private Color[] PrimColorsB = new Color[]
	{
		new Color(0F, 0F, 0F),
		new Color(0F, 0F, 0.5F),
		new Color(0F, 0F, 1F)
	};

	private Hypercube c1 = new Hypercube();
	private Hypercube c2 = new Hypercube();
	private Hypercube c3 = new Hypercube();

	private Hypercube[] originCube = new Hypercube[3];
	
	private int targetVertex;
	private bool submissionState = false;
	private int answer = 0;

	private List<string> Rotations = new List<string>()
	{
		"XY", "XZ", "XW", "YX", "YZ", "YW", "ZX", "ZY", "ZW", "WX", "WY", "WZ"
	};

	private String[,] SelectedRotations = new String[3,3];

	private bool animationPlaying = false;
	private bool stopTheRotations = false;
	private bool moduleSolved = false;

	//logging
	static int moduleIdCounter = 1;
	int moduleId;
	
	public string TwitchHelpMessage = "Use !{0} top-right-back-zag to press that vertex. Use !{0} b or !{0} bg to press the background.";
	
	void Awake(){
		moduleId = moduleIdCounter++;
		
		background.OnInteract += delegate() { BackGroundPress(background); return false; };
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
		defineRotations();
		StartCoroutine(Rotating());
	}
	void defineColors()
	{
		c1 = colorRandomizer(PrimColorsR);
		while (!validCubeCheck(c1))
		{
			c1 = colorRandomizer(PrimColorsR);
		}
		c2 = colorRandomizer(PrimColorsG);
		while (!validCubeCheck(c2))
		{
			c2 = colorRandomizer(PrimColorsG);
		}
		c3 = colorRandomizer(PrimColorsB);
		while (!validCubeCheck(c3))
		{
			c3 = colorRandomizer(PrimColorsB);
		}
		originCube[0] = new Hypercube(c1);
		originCube[1] = new Hypercube(c2);
		originCube[2] = new Hypercube(c3);

		Debug.LogFormat("[The Hypercolor #{0}] Cube Red is:", moduleId);
		DrawCube(c1);
		Debug.LogFormat("[The Hypercolor #{0}] Cube Green is", moduleId);
		DrawCube(c2);
		Debug.LogFormat("[The Hypercolor #{0}] Cube Blue is", moduleId);
		DrawCube(c3);
	}
	bool validCubeCheck(Hypercube c)
	{
		foreach (var rot in Rotations)
		{
			foreach (var rot2 in Rotations)
			{
				if (rot != rot2 && RotateCube(c, rot) == RotateCube(c, rot2))
				{
					return (false);
				}
			}
		}

		return (true);
	}
	Hypercube colorRandomizer(Color[] colors)
	{
		Hypercube temp = new Hypercube();
		
		List<Color> VertiColor = new List<Color>();
		for (int i = 0; i < 3; i++)
		{
			VertiColor.Add(colors[i]);
		}
		for (int i = 0; i < 13; i++)
		{
			VertiColor.Add(colors[UnityEngine.Random.Range(0, 3)]);
		}
		VertiColor.Shuffle();
		for (int i = 0; i < 16; i++)
		{
			temp.vert[i].VertexColor = VertiColor[i];
		}
		return (temp);
	}
	void defineRotations()
	{
		for (int i = 0; i < 3; i++)
		{
			Rotations.Shuffle();
			for (int j = 0; j < 3; j++)
			{
				SelectedRotations[i,j] = Rotations[3 * i + j];
			}
		}
		
		Debug.LogFormat("[The Hypercolor #{0}] Rotations are 1: R({1}) G({2}) B({3}) / 2: R({4}) G({5}) B({6}) / 3: R({7}) G({8}) B({9})",
			moduleId, SelectedRotations[0,0], SelectedRotations[0,1], SelectedRotations[0,2]
			, SelectedRotations[1,0], SelectedRotations[1,1], SelectedRotations[1,2]
			, SelectedRotations[2,0], SelectedRotations[2,1], SelectedRotations[2,2]);
	}
	IEnumerator Rotating()
	{
		c1 = new Hypercube(originCube[0]);
		c2 = new Hypercube(originCube[1]);
		c3 = new Hypercube(originCube[2]);
		StartCoroutine(ColorTransition(false));
		for (int j = 0; j < 150 && !submissionState; j++)
		{
			yield return new WaitForSeconds(0.01f);	
			while (stopTheRotations)
				yield return new WaitForSeconds(0.01f);	
		}
		for (int i = 0; i < 3 && !submissionState; i++)
		{
			c1 = RotateCube(c1, SelectedRotations[i,0]);
			c2 = RotateCube(c2, SelectedRotations[i,1]);
			c3 = RotateCube(c3, SelectedRotations[i,2]);
			StartCoroutine(ColorTransition(false));
			for (int j = 0; j < 150 && !submissionState; j++)
			{
				yield return new WaitForSeconds(0.01f);	
				while (stopTheRotations)
					yield return new WaitForSeconds(0.01f);	
			}
		}
		for (int i = 0; i < 16 && !submissionState; i++)
		{
			c1.vert[i].VertexColor = Color.black;
			c2.vert[i].VertexColor = Color.black;
			c3.vert[i].VertexColor = Color.black;
		}
		StartCoroutine(ColorTransition(false));
		for (int i = 0; i < 200 && !submissionState; i++)
		{
			yield return new WaitForSeconds(0.01f);	
		}
		if (!submissionState)
		{
			StartCoroutine(Rotating());
		}
	}
	
	IEnumerator ColorTransition(bool havetorestart)
	{
		Color[] ColorDifs = new Color[16];
		for (int i = 0; i < 16; i++)
		{
			ColorDifs[i] = (new Color(c1.vert[i].VertexColor.r, c2.vert[i].VertexColor.g, c3.vert[i].VertexColor.b)
			                - vertices[i].GetComponent<MeshRenderer>().material.color)/75;
		}
		for (int i = 0; i < 75; i++)
		{
			if (!submissionState)
			{
				for (int j = 0; j < 16; j++)
				{
					vertices[j].GetComponent<MeshRenderer>().material.color += ColorDifs[j];
				}
				yield return new WaitForSeconds(0.01f);	
			}
		}

		if (havetorestart)
		{
			yield return new WaitForSeconds(1f);
		}

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
	Hypercube RotateCube(Hypercube c, string rot)
	{
		Hypercube newc = new Hypercube(c);
		
		for (int i = 0; i < 4; i++)
		{
			string remainder = "XYZW";
			remainder = remainder.Replace(rot[0].ToString(), string.Empty);
			remainder = remainder.Replace(rot[1].ToString(), string.Empty);
			Color temp = newc.vert[newc.GetVertex(rot, "--", remainder, i)].VertexColor;
			newc.vert[newc.GetVertex(rot, "--", remainder, i)].VertexColor =
				newc.vert[newc.GetVertex(rot, "-+", remainder, i)].VertexColor;
			newc.vert[newc.GetVertex(rot, "-+", remainder, i)].VertexColor =
				newc.vert[newc.GetVertex(rot, "++", remainder, i)].VertexColor;
			newc.vert[newc.GetVertex(rot, "++", remainder, i)].VertexColor =
				newc.vert[newc.GetVertex(rot, "+-", remainder, i)].VertexColor;
			newc.vert[newc.GetVertex(rot, "+-", remainder, i)].VertexColor = temp;
		}

		return newc;
	}
	void DrawCube(Hypercube cube)
	{
		Debug.LogFormat("[The Hypercolor #{0}]⠀⠀⠀{1}/{2}⠀-----⠀{3}/{4}", moduleId
			, GetColor(cube.vert[0].VertexColor), GetColor(cube.vert[1].VertexColor)
			, GetColor(cube.vert[2].VertexColor), GetColor(cube.vert[3].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}]⠀⠀⠀/|⠀⠀⠀⠀⠀⠀⠀⠀/|", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]⠀⠀/⠀|⠀⠀⠀⠀⠀⠀⠀/⠀|", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]{1}/{2}⠀+----⠀{3}/{4}⠀|", moduleId
			, GetColor(cube.vert[4].VertexColor), GetColor(cube.vert[5].VertexColor)
			, GetColor(cube.vert[6].VertexColor), GetColor(cube.vert[7].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}]⠀|⠀⠀|⠀⠀⠀⠀⠀⠀|⠀⠀|", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]⠀|⠀{1}/{2}⠀----+⠀{3}/{4}", moduleId
			, GetColor(cube.vert[8].VertexColor), GetColor(cube.vert[9].VertexColor)
			, GetColor(cube.vert[10].VertexColor), GetColor(cube.vert[11].VertexColor));
		Debug.LogFormat("[The Hypercolor #{0}]⠀|⠀/⠀⠀⠀⠀⠀⠀⠀|⠀/", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]⠀|/⠀⠀⠀⠀⠀⠀⠀⠀|/", moduleId);
		Debug.LogFormat("[The Hypercolor #{0}]{1}/{2}⠀-----⠀{3}/{4}", moduleId
			, GetColor(cube.vert[12].VertexColor), GetColor(cube.vert[13].VertexColor)
			, GetColor(cube.vert[14].VertexColor), GetColor(cube.vert[15].VertexColor));
	}
	void DrawDebugCube(Hypercube cube)
	{
		Debug.LogFormat("[The Hypercolor #{0}]\n" +
		                "   {1}/{2} ----- {3}/{4}\n" +
		                "   /|        /|\n" +
		                "  / |       / |\n" +
		                "{5}/{6} +---- {7}/{8} |\n" +
		                " |  |      |  |\n" +
		                " | {9}/{10} ----+ {11}/{12}\n" +
		                " | /       | /\n" +
		                " |/        |/\n" +
		                "{13}/{14} ----- {15}/{16}", moduleId
			, GetColor(cube.vert[0].VertexColor), GetColor(cube.vert[1].VertexColor)
			, GetColor(cube.vert[2].VertexColor), GetColor(cube.vert[3].VertexColor)
			, GetColor(cube.vert[4].VertexColor), GetColor(cube.vert[5].VertexColor)
			, GetColor(cube.vert[6].VertexColor), GetColor(cube.vert[7].VertexColor)
			, GetColor(cube.vert[8].VertexColor), GetColor(cube.vert[9].VertexColor)
			, GetColor(cube.vert[10].VertexColor), GetColor(cube.vert[11].VertexColor)
			, GetColor(cube.vert[12].VertexColor), GetColor(cube.vert[13].VertexColor)
			, GetColor(cube.vert[14].VertexColor), GetColor(cube.vert[15].VertexColor));
	}
	string GetColor(Color c)
	{
		if (c == new Color(0F, 0F, 0F))
		{
			return "K";
		}
		else if (c == new Color(0F, 0F, 0.5F))
		{
			return "I";
		}
		else if (c == new Color(0F, 0F, 1F))
		{
			return "B";
		}
		else if (c == new Color(0F, 0.5F, 0F))
		{
			return "F";
		}
		else if (c == new Color(0F, 1F, 0F))
		{
			return "G";
		}
		else if (c == new Color(0.5F, 0F, 0F))
		{
			return "M";
		}
		else if (c == new Color(1F, 0F, 0F))
		{
			return "R";
		}

		return "";
	}

	IEnumerator ColorTransitionForSub()
	{
		Color[] ColorDifs = new Color[16];
		for (int i = 0; i < 16; i++)
		{
			ColorDifs[i] = (new Color(c1.vert[i].VertexColor.r, c2.vert[i].VertexColor.g, c3.vert[i].VertexColor.b)
			                - vertices[i].GetComponent<MeshRenderer>().material.color)/75;
		}
		for (int i = 0; i < 75; i++)
		{
			if (submissionState)
			{
				for (int j = 0; j < 16; j++) 
				{ 
					vertices[j].GetComponent<MeshRenderer>().material.color += ColorDifs[j];
				}
				yield return new WaitForSeconds(0.01f);
			}
		}
		yield return null;
		animationPlaying = false;
	}
	IEnumerator Submission()
	{
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				c1.vert[j].VertexColor = new Color(0.1f, 0.1f, 0.1f);
				c2.vert[j].VertexColor = new Color(0.1f, 0.1f, 0.1f);
				c3.vert[j].VertexColor = new Color(0.1f, 0.1f, 0.1f);
			}

			animationPlaying = true;
			StartCoroutine(ColorTransitionForSub());
			while (animationPlaying && submissionState) yield return true;
			
			int initialVertex = UnityEngine.Random.Range(0, 16);
			int vr = 0, vg = 0, vb = 0;
			while (vr + vg + vb == 0)
			{
				vr = UnityEngine.Random.Range(0, 3);
				vg = UnityEngine.Random.Range(0, 3);
				vb = UnityEngine.Random.Range(0, 3);
			}
			c1.vert[initialVertex].VertexColor = new Color(vr * 0.5f, vg * 0.5f, vb * 0.5f);
			c2.vert[initialVertex].VertexColor = new Color(vr * 0.5f, vg * 0.5f, vb * 0.5f);
			c3.vert[initialVertex].VertexColor = new Color(vr * 0.5f, vg * 0.5f, vb * 0.5f);
			animationPlaying = true;
			StartCoroutine(ColorTransitionForSub());
			while (animationPlaying && submissionState) yield return true;
			Hypercube final = new Hypercube(c1);
			String rots = "";
			for (int j = 0; j < vr; j++)
			{
				final = RotateCube(final, SelectedRotations[i,0]);
				rots += " " + SelectedRotations[i, 0];
			}
			for (int j = 0; j < vg; j++)
			{
				final = RotateCube(final, SelectedRotations[i, 1]);
				rots += " " + SelectedRotations[i, 1];
			}
			for (int j = 0; j < vb; j++)
			{
				final = RotateCube(final, SelectedRotations[i, 2]);
				rots += " " + SelectedRotations[i, 2];
			}
			for (int j = 0; j < 16; j++)
			{
				if (final.vert[j].VertexColor != new Color(0.1f, 0.1f, 0.1f))
					targetVertex = j;
			}
			Debug.LogFormat("[The Hypercolor #{0}] Initial vertex was in {1}, applied{2}, ended to {3}", moduleId, GetVertexCoords(initialVertex), rots, GetVertexCoords(targetVertex));

			answer = 1;
			while (answer == 1 && submissionState) yield return true;
			if (answer == 0 || !submissionState)
				break;
			if (i == 2)
			{
				StartCoroutine(Solve());
			}
		}
	}

	void BackGroundPress(KMSelectable bg)
	{
		if (moduleSolved || animationPlaying){
			return;
		}
		
		bg.AddInteractionPunch(0.1F);
		if (submissionState)
			submissionState = false;
		else
			submissionState = true;

		if (submissionState)
		{
			bombAudio.PlaySoundAtTransform("togglesfx", transform);
			Debug.LogFormat("[The Hypercolor #{0}] Entering submission state...", moduleId);
			StartCoroutine(Submission());
		}
		else
		{
			bombAudio.PlaySoundAtTransform("togglebacksfx", transform);
			for (int i = 0; i < 16; i++)
			{
				c1.vert[i].VertexColor = Color.black;
				c2.vert[i].VertexColor = Color.black;
				c3.vert[i].VertexColor = Color.black;
			}
			Debug.LogFormat("[The Hypercolor #{0}] Exiting submission state...", moduleId);
			StartCoroutine(Rotating());
		}
	}
	
	void VertexPress(KMSelectable vertex, int index)
	{
		if (moduleSolved || animationPlaying){
			return;
		}

		bombAudio.PlaySoundAtTransform("vertexsfx", transform);
		vertex.AddInteractionPunch(0.1F);

		if (!submissionState)
		{
			if (stopTheRotations)
				stopTheRotations = false;
			else if (!stopTheRotations)
				stopTheRotations = true;
			return;
		}

		if (targetVertex == index)
		{
			Debug.LogFormat("[The Hypercolor #{0}] You pressed {1}. That was right, progressing stage...", moduleId, GetVertexCoords(targetVertex));
			answer = 2;
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[The Hypercolor #{0}] You have to press {1}, You pressed {2}. Strike!", moduleId, GetVertexCoords(targetVertex), GetVertexCoords(index));
			for (int i = 0; i < 16; i++)
			{
				c1.vert[i].VertexColor = Color.black;
				c2.vert[i].VertexColor = Color.black;
				c3.vert[i].VertexColor = Color.black;
			}
			StartCoroutine(ColorTransition(false));
			answer = 0;
			submissionState = false;
			StartCoroutine(Rotating());
		}
	}
	
	IEnumerator Solve()
	{
		submissionState = false;
		for (int i = 0; i < 16; i++)
		{
			c1.vert[i].VertexColor = Color.black;
			c2.vert[i].VertexColor = Color.black;
			c3.vert[i].VertexColor = Color.black;
		}
		Debug.LogFormat("[The Hypercolor #{0}] Submission is done. Module solved.", moduleId);
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

				int[] states = new int[4] {0, 0, 0, 0};
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

	public IEnumerator TwitchHandleForcedSolve()
	{
		if (!submissionState)
			background.OnInteract();
		for (int i = 0; i < 3; i++)
		{
			while (answer != 1) yield return true;
			vertices[targetVertex].GetComponent<KMSelectable>().OnInteract();
		}
		yield return null;
    }
}
