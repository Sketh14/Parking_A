
using System.Collections;

using UnityEngine;

public class Flasher : MonoBehaviour
{
    [SerializeField] private GameObject _leftlight;
    [SerializeField] private GameObject _rightlight;
    [SerializeField] private float _wait;

    void Start()
    {

        StartCoroutine(Siren());
    }

    IEnumerator Siren()
    {
        _wait = Random.Range(0.5f, 2f);
        yield return new WaitForSeconds(_wait);
        _leftlight.SetActive(false);
        _rightlight.SetActive(true);

        yield return new WaitForSeconds(_wait);
        _leftlight.SetActive(true);
        _rightlight.SetActive(false);
        StartCoroutine(Siren());
    }
}
