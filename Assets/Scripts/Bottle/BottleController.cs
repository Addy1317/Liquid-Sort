using System.Collections;
using UnityEngine;

namespace SlowpokeStudio.Bottle
{
    public class BottleController : MonoBehaviour
    {
        [SerializeField] internal LineRenderer lineRenderer;

        [Header("Bottle Colours")]
        [SerializeField] internal Color[] bottleColors;
        [SerializeField] internal SpriteRenderer bottleMaskSR;

        [Header("Animation Curve")]
        [SerializeField] internal AnimationCurve scaleAndRotationMultiplierCurve;
        [SerializeField] internal AnimationCurve fillAmountCurve;
        [SerializeField] internal AnimationCurve rotationSpeedMultiplier;

        [Header("Colour Fill Configuraation")]
        [SerializeField] internal float[] fillAmount;
        [SerializeField] internal float[] rotationValues;
        private int rotationIndex = 0;
        [Range(0, 4)] [SerializeField] internal int numberOfColorsInBottle = 4;
        [SerializeField] internal Color topColor;
        [SerializeField] internal int numberOfTopColorLayers = 1;

        [Header("Number of Colors")]
        [SerializeField] internal BottleController bottleControllerRef;
        [SerializeField] internal bool justThisBottle = false;
        private int numberOfColorsToTransfer = 0;

        [Header("Variables")]
        [SerializeField] internal float timeToRotate = 1.0f;

        [Header("Transform Point")]
        [SerializeField] internal Transform leftRotationPoint;
        [SerializeField] internal Transform rightRotationPoint;
        private Transform choosenRotationPoint;

        private float directionMultiplier = 1.0f;

        Vector3 originalPosition;
        Vector3 startPosition;
        Vector3 endPosition;

        private void Start()
        {
            bottleMaskSR.material.SetFloat("_FillAmount", fillAmount[numberOfColorsInBottle]);
            originalPosition = transform.position;
            
            UpdateColorsOnShader();
            UpdateTopColorValues();
        }

        private void Update()
        {
            //BottlePourInputs();
        }

        internal void BottlePourInputs()
        {
            if (Input.GetKeyUp(KeyCode.P) && justThisBottle == true)
            {
                UpdateTopColorValues();

                if (bottleControllerRef.FillBottleCheck(topColor))
                {
                    ChoosenRotationPointAndDirection();
                    numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - bottleControllerRef.numberOfColorsInBottle);

                    for (int i = 0; i < numberOfColorsToTransfer; i++)
                    {
                        bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
                    }
                    bottleControllerRef.UpdateColorsOnShader();
                }

                CalculateRotationIndex(4 - bottleControllerRef.numberOfColorsInBottle);
                
                StartCoroutine(MoveBottle());
            }
        }

        internal void StartColorTransfer()
        {
            ChoosenRotationPointAndDirection();
            numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - bottleControllerRef.numberOfColorsInBottle);

            for (int i = 0; i < numberOfColorsToTransfer; i++)
            {
                bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
            }
            bottleControllerRef.UpdateColorsOnShader();

            CalculateRotationIndex(4 - bottleControllerRef.numberOfColorsInBottle);

            transform.GetComponent<SpriteRenderer>().sortingOrder += 2;
            bottleMaskSR.sortingOrder += 2;

            StartCoroutine(RotateBottle());
        }

        internal IEnumerator MoveBottle()
        {
            startPosition = transform.position;

            if(choosenRotationPoint == leftRotationPoint)
            {
                endPosition = bottleControllerRef.rightRotationPoint.position;
            }
            else
            {
                endPosition = bottleControllerRef.leftRotationPoint.position;
            }

            float t = 0;

            while(t <= 1)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                t += Time.deltaTime * 2;
                yield return new WaitForEndOfFrame();
            }
            transform.position = endPosition;

            StartCoroutine(RotateBottle());
        }

        internal IEnumerator MoveBottleBack()
        {
            startPosition = transform.position;
            endPosition = originalPosition;

            float t = 0;

            while (t <= 1)
            {
                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                t += Time.deltaTime * 2;
                yield return new WaitForEndOfFrame();
            }
            transform.position = endPosition;

            transform.GetComponent<SpriteRenderer>().sortingOrder += 2;
            bottleMaskSR.sortingOrder += 2;

        }
        internal void UpdateColorsOnShader()
        {
            bottleMaskSR.material.SetColor("_C1", bottleColors[0]);
            bottleMaskSR.material.SetColor("_C2", bottleColors[1]);
            bottleMaskSR.material.SetColor("_C3", bottleColors[2]);
            bottleMaskSR.material.SetColor("_C4", bottleColors[3]);
        }

        internal IEnumerator RotateBottle()
        {
            float t = 0;
            float lerpValue;
            float angleValue;

            float lastAngleValue = 0;

            while (t< timeToRotate)
            {
                lerpValue = t / timeToRotate;
                angleValue = Mathf.Lerp(0.0f,  directionMultiplier * rotationValues[rotationIndex], lerpValue);

                transform.eulerAngles = new Vector3(0, 0, angleValue);

               // transform.RotateAround(choosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);

                bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));

                if (fillAmount[numberOfColorsInBottle]> fillAmountCurve.Evaluate(angleValue)+ 0.005f)
                {
                    if(lineRenderer.enabled == false)
                    {
                        lineRenderer.startColor = topColor;
                        lineRenderer.endColor = topColor;

                        lineRenderer.SetPosition(0, choosenRotationPoint.position);
                        lineRenderer.SetPosition(1, choosenRotationPoint.position - Vector3.up * 1.45f);

                        lineRenderer.enabled = true;
                    }
                    bottleMaskSR.material.SetFloat("_FillAmount", fillAmountCurve.Evaluate(angleValue));

                    bottleControllerRef.FillUp(fillAmountCurve.Evaluate(lastAngleValue) - fillAmountCurve.Evaluate(angleValue));

                }

                t += Time.deltaTime * rotationSpeedMultiplier.Evaluate(angleValue);

                lastAngleValue = angleValue;

                //yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(0.5f);
            }

            angleValue = directionMultiplier * rotationValues[rotationIndex];
            //transform.eulerAngles = new Vector3(0, 0, angleValue);
            bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));
            bottleMaskSR.material.SetFloat("_FillAmount", fillAmountCurve.Evaluate(angleValue));

            numberOfColorsInBottle -= numberOfColorsToTransfer;
            bottleControllerRef.numberOfColorsInBottle += numberOfColorsToTransfer;

            lineRenderer.enabled = false;
            StartCoroutine(RotateBottleBack());
        }

        internal IEnumerator RotateBottleBack()
        {
            float t = 0;
            float lerpValue;
            float angleValue;

            float lastAngleValue = directionMultiplier * rotationValues[rotationIndex];

            while (t < timeToRotate)
            {
                lerpValue = t / timeToRotate;
                angleValue = Mathf.Lerp( directionMultiplier * rotationValues[rotationIndex], 0.0f, lerpValue);

                //transform.eulerAngles = new Vector3(0, 0, angleValue);

                transform.RotateAround(choosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);
                bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));

                lastAngleValue = angleValue;

                t += Time.deltaTime;

                yield return new WaitForEndOfFrame();

            }

            UpdateTopColorValues();

            angleValue = 0;
            transform.eulerAngles = new Vector3(0, 0, angleValue);
            bottleMaskSR.material.SetFloat("_SARM", scaleAndRotationMultiplierCurve.Evaluate(angleValue));

            StartCoroutine(MoveBottleBack());
        }

        internal void UpdateTopColorValues()
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

        internal bool FillBottleCheck(Color colorToCheck)
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

        internal void CalculateRotationIndex(int numberOfEmptySpacesInSecondBottle)
        {
            rotationIndex = 3 - (numberOfColorsInBottle - Mathf.Min(numberOfEmptySpacesInSecondBottle, numberOfTopColorLayers));
        }

        internal void FillUp(float fillAmountToAdd)
        {
            bottleMaskSR.material.SetFloat("_FillAmount", bottleMaskSR.material.GetFloat("_FillAmount") + fillAmountToAdd);
        }

        internal void ChoosenRotationPointAndDirection()
        {
            if(transform.position.x > bottleControllerRef.transform.position.x)
            {
                choosenRotationPoint = leftRotationPoint;
                directionMultiplier = -1.0f;
            }
            else
            {
                choosenRotationPoint = rightRotationPoint;
                directionMultiplier = 1.0f;
            }
        }

    }
}
