using System.IO;
using MyBox;
using ReadyPlayerMe;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace ConversationMatrixTool
{
    public class CharacterHook : MonoBehaviour
    {
        [Tooltip("this name is also given to the prefab")]
        public string characterName = "";
        [ReadOnly()]
        [Tooltip("this is how we associate scriptable objects with this controller")]public string guid;
        [Tooltip("this is generated by pressing the button")]
        [ReadOnly()]public Character characterSo;
        [Tooltip("Animator controller of the character")]
        public Animator animator;
        [Tooltip("best position for an audio source is the mouth of the character")]
        public AudioSource audioSource;
        [Tooltip("the transform of the mouth")]
        public Transform mouthPosition;
        [Tooltip("SpeechBlend controls lip sync based on audio")]
        public SpeechBlend speechBlend;
        [Tooltip("controls IK functions like looking at something")]
        public CMT_IKController iKController;
        [Tooltip("visemes on this renderer are used to animate lips and face")]
        public SkinnedMeshRenderer meshRenderer;
        [HideInInspector] public GameObject speechBlendPrefab;

        public void GenerateCharacterScriptableObject()
        {
#if UNITY_EDITOR
            if (characterSo == null)
            {
                if (characterName.Length == 0) characterName = transform.name;
                characterSo = ScriptableObject.CreateInstance<Character>();
                characterSo.colour =
                    Color.HSVToRGB(Random.Range(0f, 1f), .66f, .44f); // making sure the colour is a pastel colour
                characterSo.characterName = characterName;
                string directoryPath = "Assets/Prefabs/Characters/" + characterName + "/";
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                string _name = AssetDatabase.GenerateUniqueAssetPath(directoryPath + characterName + ".asset");
                characterSo.guid = AssetDatabase.AssetPathToGUID(_name);
                guid = characterSo.guid;
                animator = GetComponent<Animator>();
                if (animator == null)
                    animator = gameObject.AddComponent(typeof(Animator)) as Animator;
                var tempGO = new GameObject("AudioSource");
                var tempTrans = tempGO.transform;
                foreach (var aS in transform.GetComponentsInChildren<AudioSource>())
                    DestroyImmediate(aS.gameObject);
                var tempAudioSource = transform.GetComponentInChildren<AudioSource>();
                audioSource = tempAudioSource != null
                    ? tempAudioSource
                    : tempGO.AddComponent(typeof(AudioSource)) as AudioSource;
                var speechBlends = GetComponentsInChildren<SpeechBlend>();
                foreach (var sp in speechBlends)
                    DestroyImmediate(sp.gameObject);
                speechBlendPrefab = (GameObject)Resources.Load("SpeechBlend", typeof(GameObject));
                var speechBlendGO = Instantiate(speechBlendPrefab, transform, true) as GameObject;
                //Debug.Assert(speechBlendGO != null, nameof(speechBlendGO) + " != null");
                speechBlend = speechBlendGO.GetComponent<SpeechBlend>();
                if (meshRenderer == null) meshRenderer = gameObject.GetMeshRenderer(MeshType.HeadMesh);
                speechBlend.headMesh = meshRenderer;
                speechBlend.voiceAudioSource = audioSource;

                // destroy voice handler - we will use our own
                var vH = GetComponent<VoiceHandler>();
                if (vH != null) DestroyImmediate(vH);

                // find and remove Eye Animation Handler of Ready Player Me. We will use our own animation handler to animate eyes.
                var EAH = GetComponent<EyeAnimationHandler>();
                if (EAH != null) DestroyImmediate(EAH);

                //move the audio source to head
                var test = false;
                foreach (Transform g in transform.GetComponentsInChildren<Transform>())
                {
                    if (test == false)
                    {
                        if (g.name.Contains("Head") || g.name.Contains("head"))
                        {
                            test = true;
                            tempTrans.position = g.position;
                            mouthPosition = g;
                            audioSource.transform.SetParent(g);
                            audioSource.transform.localPosition = Vector3.zero;
                        }
                    }
                }

                iKController = gameObject.AddComponent(typeof(CMT_IKController)) as CMT_IKController;
                animator.applyRootMotion = false;
                tempTrans.localPosition = Vector3.zero;
                AssetDatabase.CreateAsset(characterSo, _name);
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, directoryPath + characterName + ".prefab",
                    InteractionMode.AutomatedAction);
                EditorUtility.FocusProjectWindow();
            }
#endif
        }

        public CharacterHook ReturnCharacter()
        {
            return this;
        }

        private void OnValidate()
        {
            if (characterSo == null) return;
            characterSo.animator = animator.runtimeAnimatorController;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = characterSo != null ? characterSo.colour : Color.yellow;
            Gizmos.DrawWireSphere(mouthPosition != null ? mouthPosition.position : Vector3.zero, .25f);
        }

        public void TryGetMouthPosition()
        {
            if (characterName.Length <= 0) characterName = transform.name;
            if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
            if (mouthPosition != null && audioSource != null) mouthPosition = audioSource.transform;
        }
    }
}