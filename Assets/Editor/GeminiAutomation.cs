using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class GeminiCoPilot : EditorWindow
{
    private string apiKey = "AQ.Ab8RN6IEiDNm7L5rIAMNBzkpKixrU-615BpV5qDLF9jYtbZ7zg";
    private string prompt = "Buatkan lantai Plane bernama Ground, lalu buat Player bertipe Capsule di atasnya. Beri Player komponen Rigidbody, dan atur kamera isometrik melihat ke Player.";
    private string responseText = "";
    private Vector2 scrollPos; // Untuk UI scroll

    [MenuItem("Tools/Gemini AI Co-Pilot Pro")]
    public static void ShowWindow()
    {
        GetWindow<GeminiCoPilot>("AI Co-Pilot");
    }

    void OnGUI()
    {
        GUILayout.Label("Instruksi Co-Pilot (Manipulasi Scene)", EditorStyles.boldLabel);
        prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(80));

        if (GUILayout.Button("Eksekusi Perintah", GUILayout.Height(35)))
        {
            _ = ExecuteRequestAsync();
        }

        GUILayout.Label("Console Log (Hasil Eksekusi):", EditorStyles.boldLabel);
        // Membungkus response text di dalam scroll view agar nyaman dibaca
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        responseText = EditorGUILayout.TextArea(responseText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private async Task ExecuteRequestAsync()
    {
        responseText = "Menghubungi AI... (Mohon tunggu beberapa detik)";
        Repaint();

        // Menggunakan model Flash terbaru untuk kecepatan dan keandalan
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=" + apiKey;

        string systemContext = @"Kamu adalah AI Assistant Unity Editor.
Ubah instruksi user menjadi JSON array tindakan (commands). 
WAJIB menghasilkan HANYA format JSON murni tanpa basa-basi.
Daftar action: CREATE_OBJECT, SET_POSITION, SET_ROTATION, ADD_COMPONENT, SET_CAMERA_ISOMETRIC.
Daftar type (untuk CREATE_OBJECT): Empty, Cube, Sphere, Capsule, Cylinder, Plane, Quad.
Contoh output JSON:
{
  ""commands"": [
    { ""action"": ""CREATE_OBJECT"", ""name"": ""Player"", ""type"": ""Capsule"" },
    { ""action"": ""ADD_COMPONENT"", ""name"": ""Player"", ""component"": ""Rigidbody"" },
    { ""action"": ""SET_CAMERA_ISOMETRIC"", ""targetName"": ""Player"" }
  ]
}";

        // ==================== PERBAIKAN KRUSIAL ====================
        // Menggabungkan instruksi sistem dengan input dari Anda
        string combinedText = systemContext + "\n\nInstruksi User: " + prompt;

        // Membersihkan SECARA MUTLAK semua karakter yang bisa menghancurkan struktur JSON API
        string safeText = combinedText
            .Replace("\\", "\\\\") // Amankan garis miring terbalik (backslash)
            .Replace("\"", "\\\"") // Amankan tanda kutip ganda (double quotes)
            .Replace("\n", "\\n")  // Amankan enter (newline) menjadi teks harfiah \n
            .Replace("\r", "");    // Hapus karakter tersembunyi carriage return

        string jsonBody = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{safeText}\"}}]}}]}}";
        // ===========================================================

        int maxRetries = 3;
        // ... (kode ke bawahnya tetap sama persis: int currentTry = 0; dst...)
        int currentTry = 0;
        bool success = false;

        while (currentTry < maxRetries && !success)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                var operation = www.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    success = true;
                    ProcessAIResponse(www.downloadHandler.text);
                }
                else if (www.responseCode == 429)
                {
                    currentTry++;
                    responseText = $"API Sibuk (Limit 429). Otomatis menunggu dan mencoba ulang... ({currentTry}/{maxRetries})";
                    Repaint();
                    await Task.Delay(12000); // Tunggu 12 detik
                }
                else
                {
                    responseText = "API Error: " + www.error + "\n\nDetail:\n" + www.downloadHandler.text;
                    break;
                }
            }
        }

        if (!success && currentTry >= maxRetries)
        {
            responseText = "Gagal memproses karena API terus sibuk (Mencapai batas Auto-Retry).";
        }
        Repaint();
    }

    private void ProcessAIResponse(string rawJson)
    {
        try
        {
            GeminiResponse data = JsonUtility.FromJson<GeminiResponse>(rawJson);

            // Validasi Null Super Ketat
            if (data == null || data.candidates == null || data.candidates.Length == 0)
                throw new Exception("Struktur JSON 'candidates' kosong atau gagal diparse.");

            var parts = data.candidates[0].content?.parts;
            if (parts == null || parts.Length == 0)
                throw new Exception("Struktur JSON 'parts' kosong.");

            string rawText = parts[0].text;
            if (string.IsNullOrEmpty(rawText))
                throw new Exception("Teks konten dari Gemini kosong.");

            // Ekstraktor JSON Cerdas: Mencari awal '{' dan akhir '}' secara mutlak
            Match jsonMatch = Regex.Match(rawText, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
                throw new Exception("Tidak menemukan format JSON (kurung kurawal) di dalam respons AI.");

            string cleanJson = jsonMatch.Value;

            AICommandList commandList = JsonUtility.FromJson<AICommandList>(cleanJson);
            if (commandList == null || commandList.commands == null)
                throw new Exception("Berhasil menemukan JSON, namun gagal mengubahnya menjadi C# Class (AICommandList).");

            StringBuilder log = new StringBuilder();
            log.AppendLine("=== EKSEKUSI BERHASIL ===");

            // Eksekusi tiap perintah satu per satu secara aman
            foreach (var cmd in commandList.commands)
            {
                if (string.IsNullOrEmpty(cmd.action)) continue;

                string targetDisplayName = !string.IsNullOrEmpty(cmd.name) ? cmd.name : cmd.targetName;
                log.AppendLine($">> {cmd.action} | Target: {targetDisplayName}");

                ExecuteCommandSafe(cmd, log);
            }

            responseText = log.ToString();
        }
        catch (Exception ex)
        {
            responseText = $"CRITICAL PARSING ERROR:\n{ex.Message}\n\nRespons API Mentah (Debug):\n{rawJson}";
        }
    }

    // Eksekutor Aksi yang dilindungi dengan Try-Catch agar tidak mematikan operasi lain
    private void ExecuteCommandSafe(AICommand cmd, StringBuilder log)
    {
        try
        {
            GameObject targetObj = null;
            if (!string.IsNullOrEmpty(cmd.name))
            {
                targetObj = GameObject.Find(cmd.name);
            }

            switch (cmd.action)
            {
                case "CREATE_OBJECT":
                    if (targetObj == null) // Buat jika belum ada
                    {
                        if (cmd.type == "Empty")
                        {
                            targetObj = new GameObject(cmd.name);
                        }
                        // Secara dinamis mengubah string tipe menjadi Unity Enum PrimitiveType
                        else if (Enum.TryParse(cmd.type, true, out PrimitiveType pt))
                        {
                            targetObj = GameObject.CreatePrimitive(pt);
                        }
                        else
                        {
                            targetObj = new GameObject(cmd.name);
                            log.AppendLine($"   (Warning: Tipe '{cmd.type}' tidak valid. Membuat objek Empty.)");
                        }

                        if (targetObj != null)
                        {
                            targetObj.name = cmd.name;
                            Undo.RegisterCreatedObjectUndo(targetObj, $"AI Create {cmd.name}");
                        }
                    }
                    break;

                case "SET_POSITION":
                    if (targetObj != null)
                    {
                        Undo.RecordObject(targetObj.transform, "AI Move");
                        targetObj.transform.position = new Vector3(cmd.x, cmd.y, cmd.z);
                    }
                    break;

                case "SET_ROTATION":
                    if (targetObj != null)
                    {
                        Undo.RecordObject(targetObj.transform, "AI Rotate");
                        targetObj.transform.rotation = Quaternion.Euler(cmd.x, cmd.y, cmd.z);
                    }
                    break;

                case "ADD_COMPONENT":
                    if (targetObj != null)
                    {
                        if (cmd.component.Contains("Rigidbody") && targetObj.GetComponent<Rigidbody>() == null)
                            Undo.AddComponent<Rigidbody>(targetObj);
                        else if (cmd.component.Contains("BoxCollider") && targetObj.GetComponent<BoxCollider>() == null)
                            Undo.AddComponent<BoxCollider>(targetObj);
                        else if (cmd.component.Contains("CapsuleCollider") && targetObj.GetComponent<CapsuleCollider>() == null)
                            Undo.AddComponent<CapsuleCollider>(targetObj);
                        else if (cmd.component.Contains("SphereCollider") && targetObj.GetComponent<SphereCollider>() == null)
                            Undo.AddComponent<SphereCollider>(targetObj);
                    }
                    break;

                case "SET_CAMERA_ISOMETRIC":
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        Undo.RecordObject(mainCam.transform, "AI Camera Rotation");
                        Undo.RecordObject(mainCam, "AI Camera Config");

                        mainCam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
                        mainCam.orthographic = true;

                        string tName = string.IsNullOrEmpty(cmd.targetName) ? cmd.name : cmd.targetName;
                        GameObject focus = GameObject.Find(tName);
                        if (focus != null)
                        {
                            mainCam.transform.position = focus.transform.position + new Vector3(-10f, 15f, -10f);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            log.AppendLine($"   [X] ERROR pada {cmd.action}: {ex.Message}");
        }
    }
}

// =========================================================
// DATA STRUCTURES
// =========================================================

[System.Serializable]
public class AICommandList { public AICommand[] commands; }

[System.Serializable]
public class AICommand
{
    public string action;
    public string name;
    public string type;
    public string component;
    public string targetName;
    public float x;
    public float y;
    public float z;
}

[System.Serializable] public class GeminiResponse { public Candidate[] candidates; }
[System.Serializable] public class Candidate { public Content content; }
[System.Serializable] public class Content { public Part[] parts; }
[System.Serializable] public class Part { public string text; }