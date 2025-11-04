using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TestOreInformation : MonoBehaviour
{
    [SerializeField] private OreMineable _oreMineable;
    [SerializeField] TextMeshProUGUI _text;
    void Update()
    {
        _text.text = $"Stability - {_oreMineable.Stability} \n Durability {_oreMineable.Durability}";
    }
}
