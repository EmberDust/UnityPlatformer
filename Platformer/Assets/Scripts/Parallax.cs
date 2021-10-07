using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] float _parallax = 0.5f;
    [SerializeField] float _verticalParallax = 0.1f;
    [SerializeField] bool _scrolling = false;

    [HideInInspector] public bool originalObject = true;
    
    GameObject _leftPart;
    GameObject _centerPart;
    GameObject _rightPart;

    Vector2 _centerBGOrigin;

    float _spriteWidth;
    float _partsOffset;

    GameObject _mainCamera;

    Vector2 _cameraOrigin;

    void Start()
    {
        if (originalObject)
        {
            _mainCamera = Camera.main.gameObject;
            _cameraOrigin = _mainCamera.transform.position;

            _centerPart = this.gameObject;
            _centerBGOrigin = _centerPart.transform.position;

            if (_scrolling)
            {
                _spriteWidth = GetComponent<SpriteRenderer>().sprite.bounds.size.x;
                _partsOffset = _spriteWidth * transform.localScale.x;

                _leftPart = Instantiate(this.gameObject, transform.parent);
                _leftPart.GetComponent<Parallax>().originalObject = false;
                _leftPart.transform.position = new Vector2(transform.position.x - _partsOffset, transform.position.y);

                _rightPart = Instantiate(this.gameObject, transform.parent);
                _rightPart.GetComponent<Parallax>().originalObject = false;
                _rightPart.transform.position = new Vector2(transform.position.x + _partsOffset, transform.position.y);
            }
        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        if (originalObject)
        {
            Vector2 cameraPosition = _mainCamera.transform.position;

            if (_scrolling)
            {
                if (cameraPosition.x < _leftPart.transform.position.x)
                {
                    ScrollLeft();
                }
                else if (cameraPosition.x > _rightPart.transform.position.x)
                {
                    ScrollRight();
                }
            }

            Vector2 newBackgroundPosition = new Vector2(_centerBGOrigin.x + (cameraPosition.x - _cameraOrigin.x) * _parallax,
                                                         _centerBGOrigin.y + (cameraPosition.y - _cameraOrigin.y) * _verticalParallax);

            _centerPart.transform.position = newBackgroundPosition;

            if (_scrolling)
            {
                _leftPart.transform.position = newBackgroundPosition - new Vector2(_partsOffset, 0.0f);
                _rightPart.transform.position = newBackgroundPosition + new Vector2(_partsOffset, 0.0f);
            }
        }
    }

    void ScrollLeft()
    {
        GameObject rightPart = _rightPart;

        _rightPart = _centerPart;

        _centerPart = _leftPart;
        _centerBGOrigin -= new Vector2(_partsOffset, 0.0f);

        _leftPart = rightPart;
    }

    void ScrollRight()
    {
        GameObject leftPart = _leftPart;

        _leftPart = _centerPart;

        _centerPart = _rightPart;
        _centerBGOrigin += new Vector2(_partsOffset, 0.0f);

        _rightPart = leftPart;
    }
}
