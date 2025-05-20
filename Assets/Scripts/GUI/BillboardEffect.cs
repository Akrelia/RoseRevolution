using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // Référence à la caméra principale
    [SerializeField] private float referenceDistance = 10f; // Distance de référence pour la taille
    [SerializeField] private Vector3 baseScale = Vector3.one; // Échelle de base de l'UI
    [SerializeField] private Vector3 baseOffset = Vector3.zero; // Offset de base par rapport au parent

    private Transform parentTransform; // Référence au parent (le personnage)

    private void Start()
    {
        // Si aucune caméra n'est assignée, utilise la caméra principale
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Récupérer la référence au parent
        parentTransform = transform.parent;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || parentTransform == null) return;

        // Faire face à la caméra (effet Billboard)
        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                       targetCamera.transform.rotation * Vector3.up);

        // Calculer la distance entre l'UI et la caméra
        float distance = Vector3.Distance(transform.position, targetCamera.transform.position);

        // Ajuster l'échelle pour maintenir une taille constante
        float scaleFactor = distance / referenceDistance;
        transform.localScale = baseScale * scaleFactor;

        // Ajuster la position avec un offset constant en espace écran
        // Convertir l'offset de base en espace local du parent, puis l'appliquer
        Vector3 worldOffset = parentTransform.TransformVector(baseOffset);
        transform.position = parentTransform.position + worldOffset;
    }
}