using SlowpokeStudio.Bottle;
using UnityEngine;

namespace SlowpokeStudio
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] internal BottleController firstBottle;
        [SerializeField] internal BottleController secondBottle;

        private void Update()
        {
            playerTouchInput();
        }

        private void playerTouchInput()
        {
            if(Input.GetMouseButtonDown(0))
            {
                Debug.Log("Touch Detected");
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

                RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

                if(hit.collider != null)
                {
                    if(hit.collider.GetComponent<BottleController>()!= null)
                    {
                        if(firstBottle == null)
                        {
                            firstBottle = hit.collider.GetComponent<BottleController>();
                        }
                        else
                        {
                            if(firstBottle == hit.collider.GetComponent<BottleController>())
                            {
                                firstBottle = null;
                            }
                            else
                            {
                                secondBottle = hit.collider.GetComponent<BottleController>();
                                firstBottle.bottleControllerRef = secondBottle;

                                firstBottle.UpdateTopColorValues();
                                secondBottle.UpdateTopColorValues();

                                if(secondBottle.FillBottleCheck(firstBottle.topColor)==true)
                                {
                                    firstBottle.StartColorTransfer();
                                    firstBottle = null;
                                    secondBottle = null;
                                }
                                else
                                {
                                    firstBottle = null;
                                    secondBottle = null;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
