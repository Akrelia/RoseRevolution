using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // R�f�rence � la cam�ra principale
    [SerializeField] private float referenceDistance = 10f; // Distance de r�f�rence pour la taille
    [SerializeField] private Vector3 baseScale = Vector3.one; // �chelle de base de l'UI
    [SerializeField] private Vector3 baseOffset = Vector3.zero; // Offset de base par rapport au parent

    private Transform parentTransform; // R�f�rence au parent (le personnage)

    private void Start()
    {
        // Si aucune cam�ra n'est assign�e, utilise la cam�ra principale
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // R�cup�rer la r�f�rence au parent
        parentTransform = transform.parent;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || parentTransform == null) return;

        // Faire face � la cam�ra (effet Billboard)
        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                       targetCamera.transform.rotation * Vector3.up);

        // Calculer la distance entre l'UI et la cam�ra
        float distance = Vector3.Distance(transform.position, targetCamera.transform.position);

        // Ajuster l'�chelle pour maintenir une taille constante
        float scaleFactor = distance / referenceDistance;
        transform.localScale = baseScale * scaleFactor;

        // Ajuster la position avec un offset constant en espace �cran
        // Convertir l'offset de base en espace local du parent, puis l'appliquer
        Vector3 worldOffset = parentTransform.TransformVector(baseOffset);
        transform.position = parentTransform.position + worldOffset;
    }
}