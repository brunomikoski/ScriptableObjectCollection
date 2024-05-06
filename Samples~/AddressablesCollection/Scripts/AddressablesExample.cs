using BrunoMikoski.Templates;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace BrunoMikoski.Project
{
    public class AddressablesExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] 
        private Button loadCollectionButton;
        [SerializeField] 
        private Button unloadCollectionButton;
        [SerializeField] 
        private Button loadCardButton;
        [SerializeField] 
        private Button loadDefinedCarButton;

        [SerializeField] 
        private GameObject carRootPosition;

        
        [Header("Collections")]
        [SerializeField]
        private AssetReference cardCollectionReference;

        [SerializeField] 
        private CarIDIndirectReference definedCardID;

        private void Awake()
        {
            loadCollectionButton.onClick.AddListener(OnLoadCollectionButtonClick);
            unloadCollectionButton.onClick.AddListener(OnUnloadCollectionButtonClick);
            loadCardButton.onClick.AddListener(OnLoadCardButtonClick);
            loadDefinedCarButton.onClick.AddListener(OnLoadDefinedCarButtonClick);
            
            Debug.Log(CarID.IsCollectionLoaded());
        }


        private void OnDestroy()
        {
            loadCollectionButton.onClick.RemoveListener(OnLoadCollectionButtonClick);
            unloadCollectionButton.onClick.RemoveListener(OnUnloadCollectionButtonClick);
            loadCardButton.onClick.RemoveListener(OnLoadCardButtonClick);
            loadDefinedCarButton.onClick.RemoveListener(OnLoadDefinedCarButtonClick);
        }
        
        private void OnLoadDefinedCarButtonClick()
        {
            if (!definedCardID.IsValid())
                return;
            
            LoadCarPrefab(definedCardID.Ref);
        }
        
        private void OnLoadCardButtonClick()
        {
            int nextCarIndex = carRootPosition.transform.childCount % CarID.Values.Count;
            CarID cardID = CarID.Values[nextCarIndex];
            if (cardID != null)
            {
                LoadCarPrefab(cardID);
            }
        }

        public void LoadCarPrefab(CarID targetCardID)
        {
            GameObject carTransform = Instantiate(targetCardID.CarPrefab, carRootPosition.transform);
            carTransform.transform.localPosition = new Vector3(carRootPosition.transform.childCount * -2, 0, 0);
        }

        private void OnUnloadCollectionButtonClick()
        {
            CarID.UnloadCollection();
            Debug.Log("Collection Unloaded");
            Debug.Log($"Is Collection Loaded: {CarID.IsCollectionLoaded()}");
            Assert.IsNull(CarID.Values, "Values should be null after unloading");
        }

        private void OnLoadCollectionButtonClick()
        {
            CarID.LoadCollectionAsync().Completed += _ =>
            {
                Debug.Log("Collection Loaded");
                Debug.Log($"Is Collection Loaded: {CarID.IsCollectionLoaded()}");
                Debug.Log($"Collection Count: {CarID.Values.Count}");
            };
        }
    }
}
