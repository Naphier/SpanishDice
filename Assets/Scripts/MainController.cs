using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainController : MonoBehaviour
{
    public string fileName = "data.csv";
    private string filePath
    {
        get { return Application.dataPath; }
    }

    public Rigidbody[] dice;
    public TextMeshHolder[] textMeshHolders;
    public Text errorText;

    public float force = 100f;
    public Vector3 forceDirection = Vector3.up;
    public float torque = 10f;

    public AudioSource diceSound;

    List<string> words = new List<string>();

    private float diceWidth = float.NaN;
    private float initialTextScale = float.NaN;

    private bool shouldCheckIfRollFinished = false;
    public float minimumTimeToWaitforRollFinished = 0.5f;
    private float rollTime = 0f;

    public Text resultText;

	void Start ()
    {
        diceWidth = GetDieMeshWidth();

        LoadData();
        RandomizeWords();
	}


    void Update()
    {
        if (CheckRollFinished())
            DisplayRollResults();
    }

    private bool CheckRollFinished()
    {
        bool isRollFinished = false;

        if (shouldCheckIfRollFinished && 
            (Time.time - rollTime > minimumTimeToWaitforRollFinished))
        {
            bool isRolling = false;

            if (dice == null || dice.Length <= 0)
                return true;

            foreach (Rigidbody die in dice)
            {
                if (die.velocity.sqrMagnitude > 0.01f)
                {
                    isRolling = true;
                    break;
                }
            }

            if (!isRolling)
            {
                shouldCheckIfRollFinished = false;
                isRollFinished = true;
            }
        }

        return isRollFinished;
    }

    private void DisplayRollResults()
    {
        if (resultText == null)
            return;

        if (textMeshHolders == null || textMeshHolders.Length <= 0)
            return;

        string[] results = new string[textMeshHolders.Length];

        for (int i = 0; i < textMeshHolders.Length; i++)
        {
            if (textMeshHolders[i].textMeshes == null)
                continue;

            float maxPosition = float.MinValue;
            TextMesh highestTextMesh = null;

            foreach (TextMesh textMesh in textMeshHolders[i].textMeshes)
            {
                if (textMesh.transform.position.y > maxPosition)
                {
                    maxPosition = textMesh.transform.position.y;
                    highestTextMesh = textMesh;
                }
            }

            if (highestTextMesh != null)
            {
                results[i] = highestTextMesh.text;
            }
        }


        resultText.text = string.Join("\n", results);
            
    }

    /// <summary>
    /// Use this to find the width of the die so that we can
    /// shrink the text if need be. Will only work on a cube.
    /// Also assumes that both dice are the same size.
    /// Returns float.NaN on error.
    /// </summary>
    private float GetDieMeshWidth()
    {
        float width = float.NaN;
        if (dice != null && dice.Length >= 1)
        {
            Collider collider = dice[0].gameObject.GetComponent<Collider>();

            if (collider != null)
            {
                width = collider.bounds.size.x * 0.8f; // use 80% for some extra padding
            }
        }

        //Debug.Log("width: " + width.ToString());
        return width;
    }

    void LoadData()
    {
        DataController dataController = new DataController();
        words = dataController.GetFileLines(filePath, fileName);
        if (words == null || words.Count <= 0 || !string.IsNullOrEmpty(dataController.error))
        {
            if (errorText != null)
            {
                errorText.transform.parent.gameObject.SetActive(true);
                errorText.text = dataController.error;
            }
        }
    }
	
    // Attached to 'Roll' UI button
	public void Roll()
    {
        if (dice == null || dice.Length <= 0)
        {
            Debug.LogError("dice rigidbodies not assigned in inspector.");
            return;
        }

        foreach (Rigidbody item in dice)
        {
            item.AddForce(forceDirection * force, ForceMode.Impulse);
            item.maxAngularVelocity = (Vector3.one * torque).magnitude;
            Vector3 torqueVector = RandomNormalizedVector() * torque;
            item.AddTorque(torqueVector, ForceMode.Impulse);

            Debug.LogFormat("torqueVector: {0}", torqueVector);
        }

        if (diceSound != null)
            diceSound.Play();

        shouldCheckIfRollFinished = true;
        rollTime = Time.time;

    }


    // Attached to 'Randomize' UI button
    public void RandomizeWords()
    {
        if (words == null || words.Count <= 0)
            return;

        if (textMeshHolders == null)
            return;

        words.Shuffle();
        int wordsIndex = 0;

        for (int i = 0; i < textMeshHolders.Length; i++)
        {
            if (textMeshHolders[i].textMeshes == null ||
                textMeshHolders[i].textMeshes.Length <= 0)
                continue;

            for (int j = 0; j < textMeshHolders[i].textMeshes.Length; j++)
            {
                if (textMeshHolders[i].textMeshes[j] == null)
                    continue;

                textMeshHolders[i].textMeshes[j].text = words[wordsIndex];

                wordsIndex++;
                if (wordsIndex >= words.Count)
                    wordsIndex = 0;

                FitTextMeshToCube(textMeshHolders[i].textMeshes[j]);
            }
        }
    }


    /// <summary>
    /// Check to see if we should shrink the text to fit or set it back to original size.
    /// Does not work 100% of the time. Not sure why. Could be that Unity sometimes needs to render the
    /// text mesh before it can get the width correctly?
    /// Could move this to LateUpdate and see if that helps.
    /// </summary>
    void FitTextMeshToCube(TextMesh textMesh)
    {
        if (!float.IsNaN(diceWidth))
        {
            MeshRenderer meshRenderer =
                textMesh.gameObject.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                Quaternion initialRotation = meshRenderer.transform.rotation;
                meshRenderer.transform.rotation = Quaternion.identity;

                float width = meshRenderer.bounds.size.x;

                meshRenderer.transform.rotation = initialRotation;

                if (float.IsNaN(initialTextScale))
                    initialTextScale = meshRenderer.transform.localScale.x;

                float newScale = initialTextScale;

                if (width > diceWidth)
                {
                    newScale = (diceWidth / width) * initialTextScale;
                }

                /*
                Debug.LogFormat("Text width: {0}  initialTextScale: {1}  newScale: {2}  word: {3}", 
                    width, initialTextScale, newScale, textMesh.text);
                */
                meshRenderer.transform.localScale = new Vector3(newScale, newScale, 1f);
            }
        }
    }

    private Vector3 RandomNormalizedVector()
    {
        Vector3 vector = new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));

        if (Mathf.Approximately(vector.sqrMagnitude, 0))
            vector.x = 1;

        return vector;
    }
}
