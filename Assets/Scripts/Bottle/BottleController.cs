using System.Collections;
using UnityEngine;

namespace SlowpokeStudio.Bottle
{
    public class BottleController : MonoBehaviour
    {
        [Header("Bottle Colours")]
        [SerializeField] internal Color[] bottleColors;
        [SerializeField] internal SpriteRenderer bottleMaskSR;

        [Header("Variables")]
        [SerializeField] internal float timeToRotate = 1.0f;

        [Header("Animation Curve")]
        [SerializeField] internal AnimationCurve scaleAndRotationMultiplierCurve;
        [SerializeField] internal AnimationCurve fillAmountCurve;
        [SerializeField] internal AnimationCurve rotationSpeedMultiplier;

        [Header("Colour Fill Configuraation")]
        [SerializeField] internal float[] fillAmount;
        [SerializeField] internal float[] rotationValues;
        [Range(0, 4)] [SerializeField] internal int numberOfColorsInBottle = 4;
        [SerializeField] internal Color topColor;
        [SerializeField] internal int numberOfTopColorLayers = 1;
        private int rotationIndex = 0;

        [Header("Number of Colors")]
        [SerializeField] internal BottleController bottleControllerRef;
        [SerializeField] internal bool justThidBottle = false;
        private int numberOfColorsToTransfer = 0;

        private void Start()
        {
            bottleMaskSR.material.SetFloat("_FillAmount", fillAmount[numberOfColorsInBottle]);
            UpdateColorsOnShader();
            UpdateTopColorValues();
        }

        private void Update()
        {
            if(Input.GetKeyUp(KeyCode.P) && justThidBottle == true)
            {
                UpdateTopColorValues();

                if(bottleControllerRef.FillBottleCheck(topColor))
                {
                    numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - bottleControllerRef.numberOfColorsInBottle);

                    for(int i = 0; i < numberOfColorsToTransfer; i++)
                    {
                        bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
                    }
                    bottleControllerRef.UpdateColorsOnShader();
                }
                CalculateRotationIndex(4 - bottleControllerRef.numberOfColorsInBottle);
                StartCoroutine(RotateBottle());
            }
        }

        private void UpdateColorsOnShader()
        {
            bottleMaskSR.material.SetColor("_C1", bottleColors[0]);
            bottleMaskSR.material.SetColor("_C2", bottleColors[1]);
            bottleMaskSR.material.SetColor("_C3", bottleColors[2]);
            bottleMaskSR.material.SetColor("_C4", bottleColors[3]);
        }

        private IEnumerator RotateBottle()
        {
            float t = 0;
            float lerpValue;
            float angleValue;

            float lastAngleValue = 0; 

            while(t< timeToRotate)
            {
                lerpValue = t / timeToRotate;
                angleValue = Mathf.Lerp(0.0f, rotationValues[rotationIndex], lerpValue);

                transform.eulerAngles = new Vector3(0, 0, angleValue);
                bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));

                if (fillAmount[numberOfColorsInBottle]> fillAmountCurve.Evaluate(angleValue))
                {
                    bottleMaskSR.material.SetFloat("_FillAmount", fillAmountCurve.Evaluate(angleValue));

                    bottleControllerRef.FillUp(fillAmountCurve.Evaluate(lastAngleValue) - fillAmountCurve.Evaluate(angleValue));
                }

                t += Time.deltaTime *rotationSpeedMultiplier.Evaluate(angleValue);

                lastAngleValue = angleValue;

                yield return null;//new WaitForEndOfFrame();
            }

            angleValue = rotationValues[rotationIndex];
            transform.eulerAngles = new Vector3(0, 0, angleValue);
            bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));
            bottleMaskSR.material.SetFloat("_FillAmount", fillAmountCurve.Evaluate(angleValue));

            numberOfColorsInBottle -= numberOfColorsToTransfer;
            bottleControllerRef.numberOfColorsInBottle += numberOfColorsToTransfer;

            StartCoroutine(RotateBottleBack());
        }

        private IEnumerator RotateBottleBack()
        {
            float t = 0;
            float lerpValue;
            float angleValue;

            while (t < timeToRotate)
            {
                lerpValue = t / timeToRotate;
                angleValue = Mathf.Lerp(rotationValues[rotationIndex], 0.0f, lerpValue);

                transform.eulerAngles = new Vector3(0, 0, angleValue);
                bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));

                t += Time.deltaTime;

                yield return null;//new WaitForEndOfFrame();

            }

            UpdateTopColorValues();

            angleValue = rotationValues[rotationIndex];
            transform.eulerAngles = new Vector3(0, 0, angleValue);
            bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));
        }

        private void UpdateTopColorValues()
        {
            if (numberOfColorsInBottle!=0)
            {
                numberOfTopColorLayers = 1;

                topColor = bottleColors[numberOfColorsInBottle - 1];

                if(numberOfColorsInBottle == 4 )
                {
                    if (bottleColors[3].Equals(bottleColors[2]))
                    {
                        numberOfTopColorLayers = 2;
                        if (bottleColors[2].Equals(bottleColors[1]))
                        {
                            numberOfTopColorLayers = 3;
                            if (bottleColors[1].Equals(bottleColors[0]))
                            {
                                numberOfTopColorLayers = 4;
                            }
                        }
                    }
                }

                else if (numberOfColorsInBottle == 3)
                {
                    if (bottleColors[2].Equals(bottleColors[1]))
                    {
                        numberOfTopColorLayers = 2;
                        if (bottleColors[1].Equals(bottleColors[0]))
                        {
                            numberOfTopColorLayers = 3;
                            
                        }
                    }
                }

                else if (numberOfColorsInBottle == 2)
                {
                    if (bottleColors[1].Equals(bottleColors[0]))
                    {
                        numberOfTopColorLayers = 2;
                        
                    }
                }

                rotationIndex = 3 - (numberOfColorsInBottle - numberOfTopColorLayers);
            }
        }

        private bool FillBottleCheck(Color colorToCheck)
        {
            if(numberOfColorsInBottle == 0)
            {
                return true;
            }
            else
            {
                if(numberOfColorsInBottle == 4)
                {
                    return false;
                }
                else
                {
                    if(topColor.Equals(colorToCheck))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

        }

        private void CalculateRotationIndex(int numberOfEmptySpacesInSecondBottle)
        {
            rotationIndex = 3 - (numberOfColorsInBottle - Mathf.Min(numberOfEmptySpacesInSecondBottle, numberOfTopColorLayers));
        }

        private void FillUp(float fillAmountToAdd)
        {
            bottleMaskSR.material.SetFloat("_FillAmount", bottleMaskSR.material.GetFloat("_FillAmount") + fillAmountToAdd);
        }
    }
}
