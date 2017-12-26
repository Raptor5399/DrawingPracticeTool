using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Drawing;

public class AppManager : MonoBehaviour
{
    private Texture2D[] textureArray;
    public InputField TimePerImage;
    public InputField ImageDirectory;
    public Button Start;
    public Button Stop;
    public RawImage GUIImage;
    public UnityEngine.UI.Image Progress;
    private bool practicing;
    public Text Countdown;
    private float CountdownTime;
    private SaveData saveData;
    public AudioClip valid;  
    public AudioClip invalid;
    private AudioSource audioSource;

    private void Awake()
    {
        Start.onClick.AddListener(() => StartPractice());
        Stop.onClick.AddListener(() => StopPractice());
        CountdownTime = 5.99f;
        LoadSaveData();
        audioSource = Camera.main.GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        SaveSaveData();
    }

    private void StartPractice()
    {
        if (string.IsNullOrEmpty(TimePerImage.text) || string.IsNullOrEmpty(ImageDirectory.text) || Convert.ToInt32(TimePerImage.text) < 1)
        {
            audioSource.PlayOneShot(invalid, 0.5f);
            return;
        }
        audioSource.PlayOneShot(valid, 0.5f);

        if (practicing)
        {
            StopPractice();
        }
        StartCoroutine("Practice");
    }

    private void StopPractice()
    {
        if (!practicing)
        {
            return;
        }

        StopCoroutine("Practice");
        practicing = false;
        Countdown.gameObject.SetActive(false);
        GUIImage.gameObject.SetActive(false);
    }

    private IEnumerator LoadImagesFromDirectory()
    {
        string[] fileArray = Directory.GetFiles(saveData.ImageDirectory);
        textureArray = new Texture2D[fileArray.Length];

        for (int i = 0; i < fileArray.Length; i++)
        {
            Countdown.text = string.Format("Loading {0} of {1}", i, fileArray.Length);

            Bitmap img = new Bitmap(fileArray[i]);
            var imageWidth = img.Width;
            var imageHeight = img.Height;

            WWW www = new WWW("file://" + fileArray[i]);
            yield return www;

            Texture2D texTmp = new Texture2D(imageWidth, imageHeight, TextureFormat.DXT1, false);
            www.LoadImageIntoTexture(texTmp);
            textureArray[i] = texTmp;
        }

        ShuffleArray(textureArray);
    }

    private IEnumerator Practice()
    {
        Countdown.gameObject.SetActive(true);

        yield return LoadImagesFromDirectory();

        float timer = CountdownTime;
        while (timer > 1)
        {
            timer -= Time.deltaTime;
            Countdown.text = string.Format("Starting in {0}", ((int)timer).ToString());
            yield return new WaitForEndOfFrame();
        }

        Countdown.gameObject.SetActive(false);
        GUIImage.gameObject.SetActive(true);

        practicing = true;

        for (int i = 0; i < textureArray.Length - 1; i++)
        {
            var temp = (Texture2D)textureArray[i];
            SetImage(temp);

            yield return new WaitForSeconds(Convert.ToInt32(TimePerImage.text));
            yield return ChangeToNextImage();
        }

        GUIImage.gameObject.SetActive(true);
        StartPractice(); //Keep looping through so practice ends only when pressing the stop button
    }

    private IEnumerator ChangeToNextImage()
    {
        Countdown.gameObject.SetActive(true);
        GUIImage.gameObject.SetActive(false);

        float timer = CountdownTime;
        while (timer > 1)
        {
            timer -= Time.deltaTime;
            Countdown.text = string.Format("Next in {0}", ((int)timer).ToString());
            yield return new WaitForEndOfFrame();
        }

        Countdown.gameObject.SetActive(false);
        GUIImage.gameObject.SetActive(true);
    }

    private void SetImage(Texture2D currentTexture)
    {
        GUIImage.texture = currentTexture;
        var rectSize = GUIImage.rectTransform.sizeDelta = new Vector2(currentTexture.width, currentTexture.height);
        while (rectSize.x > Camera.main.pixelWidth || rectSize.y > Camera.main.pixelHeight)
        {
            rectSize.x = rectSize.x * 0.99f;
            rectSize.y = rectSize.y * 0.99f;
        }
        GUIImage.rectTransform.sizeDelta = rectSize;
        Progress.fillAmount = 1f;
        Debug.Log("currentTexture.width " + currentTexture.width);
        Debug.Log("currentTexture.height " + currentTexture.height);
        Debug.Log("rectSize.x " + rectSize.x);
        Debug.Log("rectSize.y " + rectSize.y);
    }

    private void Update()
    {
        if (practicing)
        {
            Progress.fillAmount -= 1f / Convert.ToInt32(TimePerImage.text) * Time.deltaTime;
        }
    }

    public static void ShuffleArray<T>(T[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            T tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
    }

    public void LoadSaveData()
    {
        if (File.Exists(Application.dataPath + "/DrawingPracticeSaveData.json"))
        {
            string dataAsJson = File.ReadAllText(Application.dataPath + "/DrawingPracticeSaveData.json");
            saveData = JsonUtility.FromJson<SaveData>(dataAsJson);

            TimePerImage.text = saveData.TimePerImage;
            ImageDirectory.text = saveData.ImageDirectory;

            return;
        }
        saveData = new SaveData();
    }
    public void SaveSaveData()
    {
        saveData.TimePerImage = TimePerImage.text;
        saveData.ImageDirectory = ImageDirectory.text;

        string dataAsJson = JsonUtility.ToJson(saveData);
        File.WriteAllText(Application.dataPath + "/DrawingPracticeSaveData.json", dataAsJson);
    }
}

[Serializable]
public class SaveData
{
    public SaveData()
    {
        TimePerImage = "";
        ImageDirectory = "";
    }

    public string TimePerImage;
    public string ImageDirectory;
}
