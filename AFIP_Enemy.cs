using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFIP_Enemy : Enemy
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float moveRange;

    [SerializeField] private Vector2 startPosition;
    [SerializeField] private bool movingForward;
    [SerializeField] private bool startingDirectionRight;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        movingForward = true;
    }

    void FixedUpdate()
    {
        MoveEnemy();
    }

    void MoveEnemy()
    {
        if(moveSpeed == 0 || moveRange == 0)
        {
            return;
        }



        if(startingDirectionRight)
        {
            float moveDirection = movingForward ? 1f : -1f;
            transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);
            spriteRenderer.flipX = !movingForward;

            if (movingForward && transform.position.x >= startPosition.x + moveRange)
            {
                movingForward = false;
            }
            else if (!movingForward && transform.position.x <= startPosition.x)
            {
                movingForward = true;
            }
        }
        else
        {
            float moveDirection = movingForward ? -1f : 1f;
            transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);
            spriteRenderer.flipX = movingForward;

            if (movingForward && transform.position.x <= startPosition.x - moveRange)
            {
                movingForward = false;
            }
            else if (!movingForward && transform.position.x >= startPosition.x)
            {
                movingForward = true;
            }
        }

    }

}